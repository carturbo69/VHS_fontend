// Chatbox AI JavaScript
console.log('=== chatbox.js LOADED ===');

(function() {
    'use strict';
    console.log('=== chatbox.js IIFE STARTED ===');

    // Configuration - Tự động lấy từ backend
    // Nếu frontend và backend cùng domain, dùng relative paths
    // Nếu khác domain, có thể cấu hình trong appsettings và tạo endpoint trả về config
    const getBaseUrl = () => {
        // Thử lấy từ meta tag nếu có (backend có thể inject)
        const metaApiUrl = document.querySelector('meta[name="api-base-url"]')?.getAttribute('content');
        if (metaApiUrl) return metaApiUrl;
        
        // Fallback: dùng relative path (nếu cùng domain) hoặc localhost cho development
        // Trong production, nên dùng relative path: '/api/ChatboxAI'
        const isProduction = window.location.hostname !== 'localhost';
        if (isProduction) {
            return '/api/ChatboxAI'; // Relative path cho production
        }
        return 'http://localhost:5154/api/ChatboxAI'; // Development
    };
    
    const getSignalRUrl = () => {
        // Thử lấy từ meta tag nếu có
        const metaSignalRUrl = document.querySelector('meta[name="signalr-url"]')?.getAttribute('content');
        if (metaSignalRUrl) return metaSignalRUrl;
        
        // Fallback
        const isProduction = window.location.hostname !== 'localhost';
        if (isProduction) {
            // Dùng cùng origin với frontend
            return `${window.location.origin}/hubs/chat`;
        }
        return 'http://localhost:5154/hubs/chat'; // Development
    };

    const CONFIG = {
        apiBaseUrl: getBaseUrl(),
        signalRUrl: getSignalRUrl(),
        sessionId: localStorage.getItem('chatbox_session_id') || generateSessionId(),
        language: 'vi',
        conversationId: null
    };

    // Initialize
    let jwt = null;
    let conversationId = null;
    let isMinimized = false;
    let connection = null; // SignalR connection
    let isConnected = false;
    let hasInitialized = false; // Flag to prevent duplicate welcome messages

    // Generate unique session ID
    function generateSessionId() {
        const id = 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        localStorage.setItem('chatbox_session_id', id);
        return id;
    }

    // DOM Elements
    const elements = {
        toggle: document.getElementById('chatboxToggle'),
        container: document.getElementById('chatboxContainer'),
        messages: document.getElementById('chatboxMessages'),
        input: document.getElementById('chatboxInput'),
        sendBtn: document.getElementById('chatboxSendBtn'),
        closeBtn: document.getElementById('chatboxClose'),
        quickActions: document.getElementById('chatboxQuickActions'),
        header: document.querySelector('.chatbox-header')
    };
    
    // Drag and drop state
    let isDragging = false;
    let dragOffset = { x: 0, y: 0 };
    let currentPosition = { bottom: 100, right: 30 };
    let rafId = null;
    
    // Toggle button drag and drop state
    let isToggleDragging = false;
    let toggleDragOffset = { x: 0, y: 0 };
    let togglePosition = { bottom: 30, right: 30 };
    let toggleRafId = null;

    // Apply dark mode based on user's system preference or body class
    function applyDarkMode() {
        if (!elements.container) return;
        
        // Check if body or html has dark class
        const isDarkMode = document.body.classList.contains('dark') || 
                         document.documentElement.classList.contains('dark') ||
                         document.body.classList.contains('dark-mode') ||
                         (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
        
        if (isDarkMode) {
            elements.container.classList.add('dark');
            elements.container.classList.remove('light-mode');
        } else {
            elements.container.classList.remove('dark');
        }
        
        console.log('[Chatbox] Dark mode:', isDarkMode ? 'ON' : 'OFF');
    }

    // Initialize chatbox
    function init() {
        console.log('[Chatbox] Initializing...');
        console.log('[Chatbox] DOM Elements:', elements);
        
        // Check if elements exist
        if (!elements.toggle) {
            console.error('[Chatbox] Toggle button not found!');
            return;
        }
        
        // Apply dark mode
        applyDarkMode();
        
        // Watch for dark mode changes
        if (window.matchMedia) {
            const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
            darkModeQuery.addEventListener('change', applyDarkMode);
        }
        
        // Watch for body class changes (if user toggles dark mode)
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                    applyDarkMode();
                }
            });
        });
        
        if (document.body) {
            observer.observe(document.body, { attributes: true, attributeFilter: ['class'] });
        }
        if (document.documentElement) {
            observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
        }
        
        // Get JWT token from session or cookie
        jwt = getJWTToken();
        console.log('[Chatbox] JWT:', jwt ? 'Found' : 'Not found');
        
        // Load saved positions
        loadSavedPosition();
        loadTogglePosition();
        
        // Add event listeners
        // Note: toggle button click is handled in initToggleDragAndDrop
        elements.closeBtn.addEventListener('click', closeChatbox);
        elements.sendBtn.addEventListener('click', sendMessage);
        elements.input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
        
        // Drag and drop functionality for chatbox
        if (elements.header) {
            initDragAndDrop();
        }
        
        // Drag and drop functionality for toggle button
        initToggleDragAndDrop();

        console.log('[Chatbox] Event listeners attached');

        // Initialize SignalR connection
        initSignalR();

        // Load conversation if user is authenticated
        // Don't load here - wait for user to open chatbox first
        // This prevents duplicate welcome messages
        console.log('[Chatbox] Initialized, waiting for user to open chatbox');
    }

    // Initialize SignalR connection
    async function initSignalR() {
        try {
            // Import SignalR client (using CDN)
            if (typeof signalR === 'undefined') {
                // Load SignalR from CDN if not already loaded
                const script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js';
                script.onload = () => {
                    console.log('[Chatbox] SignalR loaded, connecting...');
                    connectSignalR();
                };
                document.head.appendChild(script);
            } else {
                connectSignalR();
            }
        } catch (error) {
            console.error('[Chatbox] Error initializing SignalR:', error);
        }
    }

    // Connect to SignalR Hub
    function connectSignalR() {
        try {
            const urlOptions = {
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
            };
            
            // Add JWT token if available
            if (jwt) {
                urlOptions.accessTokenFactory = () => jwt;
            }
            
            connection = new signalR.HubConnectionBuilder()
                .withUrl(CONFIG.signalRUrl, urlOptions)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.elapsedMilliseconds < 60000) {
                            return 2000; // Retry after 2 seconds
                        }
                        return 5000; // Retry after 5 seconds
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Connection event handlers
            connection.onclose(() => {
                console.log('[Chatbox] SignalR connection closed');
                isConnected = false;
            });

            connection.onreconnecting(() => {
                console.log('[Chatbox] SignalR reconnecting...');
            });

            connection.onreconnected(() => {
                console.log('[Chatbox] SignalR reconnected');
                isConnected = true;
                if (conversationId) {
                    connection.invoke('JoinConversation', conversationId);
                }
            });

            // Message handlers
            connection.on('MessageReceived', (data) => {
                console.log('[Chatbox] Message received via SignalR:', data);
                hideTypingIndicator();
                
                const message = data.Message || data.message || data;
                if (message) {
                    const content = message.Content || message.content || message.MessageContent || message.messageContent;
                    const senderType = message.SenderType || message.senderType || 'AI';
                    
                    // Only display AI messages (user messages are already displayed locally)
                    if (senderType === 'AI' && content) {
                        displayMessage(content, 'AI', new Date(message.CreatedAt || message.createdAt || new Date()));
                        
                        // Show quick actions if available
                        const quickActions = message.QuickActions || message.quickActions;
                        if (quickActions && quickActions.length > 0) {
                            showQuickActions(quickActions);
                        }
                    }
                }
            });

            connection.on('TypingIndicator', (data) => {
                if (data.IsTyping && data.UserId !== getUserId()) {
                    showTypingIndicator();
                } else {
                    hideTypingIndicator();
                }
            });

            connection.on('MessageError', (data) => {
                console.error('[Chatbox] Message error:', data);
                hideTypingIndicator();
                displayMessage('Xin lỗi, đã xảy ra lỗi. Vui lòng thử lại.', 'AI', new Date());
            });

            // Start connection
            connection.start()
                .then(() => {
                    console.log('[Chatbox] SignalR connected');
                    isConnected = true;
                    
                    // Join conversation if exists
                    if (conversationId) {
                        connection.invoke('JoinConversation', conversationId);
                    }
                })
                .catch(err => {
                    console.error('[Chatbox] SignalR connection error:', err);
                    isConnected = false;
                });

        } catch (error) {
            console.error('[Chatbox] Error connecting to SignalR:', error);
            isConnected = false;
        }
    }

    // Get user ID from JWT (helper function)
    function getUserId() {
        if (!jwt) return null;
        try {
            const payload = JSON.parse(atob(jwt.split('.')[1]));
            return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || null;
        } catch {
            return null;
        }
    }

    // Get JWT token
    function getJWTToken() {
        // Try multiple sources to get JWT
        // 1. Check if there's a JWT in session storage (some apps use this)
        let jwt = sessionStorage.getItem('JWToken');
        
        if (jwt) {
            console.log('[Chatbox] Got JWT from sessionStorage');
            return jwt;
        }
        
        // 2. Check cookies
        const jwtCookie = document.cookie.split('; ').find(row => row.startsWith('JWToken='));
        if (jwtCookie) {
            jwt = jwtCookie.split('=')[1];
            console.log('[Chatbox] Got JWT from cookie');
            return jwt;
        }
        
        // 3. Try to get from a global variable (if set elsewhere)
        if (window.JWT_TOKEN) {
            jwt = window.JWT_TOKEN;
            console.log('[Chatbox] Got JWT from global variable');
            return jwt;
        }
        
        console.log('[Chatbox] No JWT found');
        return null;
    }

    // Toggle chatbox
    function toggleChatbox() {
        const isOpen = elements.container.classList.contains('open');
        elements.container.classList.toggle('open');
        
        if (!isOpen) {
            // Chatbox is opening - ensure position is applied
            applyPosition(currentPosition);
            // Only load conversation if not already initialized
            // loadConversation() and showWelcomeMessage() will set hasInitialized
            if (!hasInitialized) {
                if (jwt) {
                    loadConversation();
                } else {
                    showWelcomeMessage();
                }
            }
        }
    }

    // Close chatbox
    function closeChatbox() {
        elements.container.classList.remove('open');
    }
    
    // Load saved position from localStorage
    function loadSavedPosition() {
        if (!elements.container) return;
        
        const saved = localStorage.getItem('chatbox_position');
        if (saved) {
            try {
                const pos = JSON.parse(saved);
                currentPosition = pos;
                applyPosition(pos);
                console.log('[Chatbox] Loaded saved position:', pos);
            } catch (e) {
                console.log('[Chatbox] Failed to load saved position:', e);
            }
        } else {
            // Apply default position
            applyPosition(currentPosition);
        }
    }
    
    // Save position to localStorage
    function savePosition() {
        try {
            localStorage.setItem('chatbox_position', JSON.stringify(currentPosition));
            console.log('[Chatbox] Saved position:', currentPosition);
        } catch (e) {
            console.error('[Chatbox] Failed to save position:', e);
        }
    }
    
    // Apply position to container
    function applyPosition(pos) {
        if (!elements.container) return;
        
        // Always apply position, even when container is hidden
        elements.container.style.bottom = pos.bottom + 'px';
        elements.container.style.right = pos.right + 'px';
        elements.container.style.top = 'auto';
        elements.container.style.left = 'auto';
    }
    
    // Initialize drag and drop
    function initDragAndDrop() {
        const header = elements.header;
        const container = elements.container;
        
        header.addEventListener('mousedown', startDrag);
        
        function startDrag(e) {
            // Don't drag if clicking on buttons or interactive elements
            if (e.target.closest('.chatbox-btn') || e.target.closest('.chatbox-actions')) {
                return;
            }
            
            isDragging = true;
            container.classList.add('dragging');
            
            const rect = container.getBoundingClientRect();
            // Tính offset từ điểm click đến góc trên bên trái của container
            // Để khi kéo, điểm click sẽ giữ nguyên vị trí trên container
            dragOffset.x = e.clientX - rect.left;
            dragOffset.y = e.clientY - rect.top;
            
            document.addEventListener('mousemove', onDrag);
            document.addEventListener('mouseup', stopDrag);
            
            e.preventDefault();
        }
        
        function onDrag(e) {
            if (!isDragging) return;
            
            // Cancel previous animation frame if exists
            if (rafId !== null) {
                cancelAnimationFrame(rafId);
            }
            
            // Use requestAnimationFrame for smooth updates
            rafId = requestAnimationFrame(() => {
                const windowWidth = window.innerWidth;
                const windowHeight = window.innerHeight;
                const containerWidth = container.offsetWidth;
                const containerHeight = container.offsetHeight;
                
                // Tính vị trí mới dựa trên điểm click trừ đi offset
                // Điểm click sẽ giữ nguyên vị trí trên container
                let newLeft = e.clientX - dragOffset.x;
                let newTop = e.clientY - dragOffset.y;
                
                // Chuyển đổi từ left/top sang right/bottom
                let newRight = windowWidth - newLeft - containerWidth;
                let newBottom = windowHeight - newTop - containerHeight;
                
                // Constrain to viewport
                const minRight = 10;
                const maxRight = windowWidth - containerWidth - 10;
                const minBottom = 10;
                const maxBottom = windowHeight - containerHeight - 10;
                
                newRight = Math.max(minRight, Math.min(maxRight, newRight));
                newBottom = Math.max(minBottom, Math.min(maxBottom, newBottom));
                
                currentPosition.right = newRight;
                currentPosition.bottom = newBottom;
                
                applyPosition(currentPosition);
                rafId = null;
            });
        }
        
        function stopDrag(e) {
            // Cancel any pending animation frame
            if (rafId !== null) {
                cancelAnimationFrame(rafId);
                rafId = null;
            }
            
            if (isDragging) {
                isDragging = false;
                container.classList.remove('dragging');
                
                // Ensure position is updated before saving
                const containerRect = container.getBoundingClientRect();
                const windowWidth = window.innerWidth;
                const windowHeight = window.innerHeight;
                
                let finalRight = windowWidth - containerRect.right;
                let finalBottom = windowHeight - containerRect.bottom;
                
                // Constrain to viewport
                const minRight = 10;
                const maxRight = windowWidth - containerRect.width - 10;
                const minBottom = 10;
                const maxBottom = windowHeight - containerRect.height - 10;
                
                finalRight = Math.max(minRight, Math.min(maxRight, finalRight));
                finalBottom = Math.max(minBottom, Math.min(maxBottom, finalBottom));
                
                currentPosition.right = finalRight;
                currentPosition.bottom = finalBottom;
                
                applyPosition(currentPosition);
                savePosition();
                
                console.log('[Chatbox] Drag stopped, position saved:', currentPosition);
            }
            
            document.removeEventListener('mousemove', onDrag);
            document.removeEventListener('mouseup', stopDrag);
        }
    }
    
    // Load saved toggle button position
    function loadTogglePosition() {
        if (!elements.toggle) return;
        
        const saved = localStorage.getItem('chatbox_toggle_position');
        if (saved) {
            try {
                const pos = JSON.parse(saved);
                togglePosition = pos;
                applyTogglePosition(pos);
                console.log('[Chatbox] Loaded saved toggle position:', pos);
            } catch (e) {
                console.log('[Chatbox] Failed to load saved toggle position:', e);
            }
        } else {
            applyTogglePosition(togglePosition);
        }
    }
    
    // Save toggle button position
    function saveTogglePosition() {
        try {
            localStorage.setItem('chatbox_toggle_position', JSON.stringify(togglePosition));
            console.log('[Chatbox] Saved toggle position:', togglePosition);
        } catch (e) {
            console.error('[Chatbox] Failed to save toggle position:', e);
        }
    }
    
    // Apply toggle button position
    function applyTogglePosition(pos) {
        if (!elements.toggle) return;
        elements.toggle.style.bottom = pos.bottom + 'px';
        elements.toggle.style.right = pos.right + 'px';
        elements.toggle.style.top = 'auto';
        elements.toggle.style.left = 'auto';
    }
    
    // Initialize toggle button drag and drop
    function initToggleDragAndDrop() {
        if (!elements.toggle) return;
        
        const toggle = elements.toggle;
        let startX = 0, startY = 0;
        let startRight = 0, startBottom = 0;
        
        toggle.addEventListener('mousedown', startToggleDrag);
        
        function startToggleDrag(e) {
            // Prevent default to avoid text selection
            e.preventDefault();
            
            // Get initial mouse position
            startX = e.clientX;
            startY = e.clientY;
            
            // Get initial button position
            const rect = toggle.getBoundingClientRect();
            startRight = window.innerWidth - rect.right;
            startBottom = window.innerHeight - rect.bottom;
            
            isToggleDragging = false;
            
            // Add listeners to document
            document.addEventListener('mousemove', onToggleDrag);
            document.addEventListener('mouseup', stopToggleDrag);
        }
        
        function onToggleDrag(e) {
            // Calculate how much mouse moved
            const deltaX = e.clientX - startX;
            const deltaY = e.clientY - startY;
            const moved = Math.abs(deltaX) + Math.abs(deltaY);
            
            // Only start dragging if mouse moved more than 5px
            if (!isToggleDragging) {
                if (moved < 5) return; // Not dragging yet, just a click
                isToggleDragging = true;
                toggle.classList.add('dragging');
                // Prevent click event when dragging
                toggle.style.pointerEvents = 'none';
            }
            
            // Cancel previous animation frame if exists
            if (toggleRafId !== null) {
                cancelAnimationFrame(toggleRafId);
            }
            
            // Use requestAnimationFrame for smooth updates
            toggleRafId = requestAnimationFrame(() => {
                // Calculate new position based on initial position + mouse movement
                const windowWidth = window.innerWidth;
                const windowHeight = window.innerHeight;
                
                let newRight = startRight - deltaX; // Move left when mouse moves right
                let newBottom = startBottom - deltaY; // Move up when mouse moves down
                
                // Constrain to viewport
                const minRight = 10;
                const maxRight = windowWidth - toggle.offsetWidth - 10;
                const minBottom = 10;
                const maxBottom = windowHeight - toggle.offsetHeight - 10;
                
                newRight = Math.max(minRight, Math.min(maxRight, newRight));
                newBottom = Math.max(minBottom, Math.min(maxBottom, newBottom));
                
                togglePosition.right = newRight;
                togglePosition.bottom = newBottom;
                
                applyTogglePosition(togglePosition);
                toggleRafId = null;
            });
        }
        
        function stopToggleDrag(e) {
            // Cancel any pending animation frame
            if (toggleRafId !== null) {
                cancelAnimationFrame(toggleRafId);
                toggleRafId = null;
            }
            
            // Remove listeners
            document.removeEventListener('mousemove', onToggleDrag);
            document.removeEventListener('mouseup', stopToggleDrag);
            
            if (isToggleDragging) {
                isToggleDragging = false;
                toggle.classList.remove('dragging');
                toggle.style.pointerEvents = '';
                
                // Final position adjustment
                const rect = toggle.getBoundingClientRect();
                const windowWidth = window.innerWidth;
                const windowHeight = window.innerHeight;
                
                let finalRight = windowWidth - rect.right;
                let finalBottom = windowHeight - rect.bottom;
                
                const minRight = 10;
                const maxRight = windowWidth - toggle.offsetWidth - 10;
                const minBottom = 10;
                const maxBottom = windowHeight - toggle.offsetHeight - 10;
                
                finalRight = Math.max(minRight, Math.min(maxRight, finalRight));
                finalBottom = Math.max(minBottom, Math.min(maxBottom, finalBottom));
                
                togglePosition.right = finalRight;
                togglePosition.bottom = finalBottom;
                
                applyTogglePosition(togglePosition);
                saveTogglePosition();
                
                console.log('[Chatbox] Toggle drag stopped, position saved:', togglePosition);
            } else {
                // It was just a click, toggle chatbox
                toggleChatbox();
            }
        }
    }

    // Load conversation
    async function loadConversation() {
        // Check if already initialized to prevent duplicate messages
        if (hasInitialized && elements.messages.children.length > 0) {
            console.log('[Chatbox] Already initialized, skipping loadConversation');
            return;
        }
        
        try {
            const headers = { 'Content-Type': 'application/json' };
            if (jwt) headers['Authorization'] = `Bearer ${jwt}`;

            const response = await fetch(`${CONFIG.apiBaseUrl}/conversation/active?sessionId=${CONFIG.sessionId}`, {
                method: 'GET',
                headers: headers
            });

            if (response.ok) {
                const data = await response.json();
                const convId = data.conversationId || data.ConversationId;
                if (convId) {
                    conversationId = convId;
                    
                    // Join conversation via SignalR if connected
                    if (isConnected && connection) {
                        await connection.invoke('JoinConversation', conversationId);
                    }
                    
                    // loadMessageHistory will set hasInitialized
                    await loadMessageHistory(conversationId);
                } else {
                    // Only show welcome if not already shown
                    if (!hasInitialized) {
                        showWelcomeMessage();
                        hasInitialized = true;
                    }
                }
            } else {
                // Only show welcome if not already shown
                if (!hasInitialized) {
                    showWelcomeMessage();
                    hasInitialized = true;
                }
            }
        } catch (error) {
            console.error('Error loading conversation:', error);
            // Only show welcome if not already shown
            if (!hasInitialized) {
                showWelcomeMessage();
                hasInitialized = true;
            }
        }
    }

    // Load message history
    async function loadMessageHistory(convId) {
        try {
            const response = await fetch(`${CONFIG.apiBaseUrl}/conversation/${convId}/history?page=1&pageSize=50`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${jwt}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                const messages = data.messages || [];
                if (messages.length > 0) {
                    messages.forEach(msg => {
                        displayMessage(msg.content, msg.senderType, msg.createdAt);
                    });
                    hasInitialized = true;
                } else {
                    // No messages in history, show welcome if not already shown
                    if (!hasInitialized) {
                        showWelcomeMessage();
                    }
                }
            }
        } catch (error) {
            console.error('Error loading history:', error);
            // If error and not initialized, show welcome
            if (!hasInitialized) {
                showWelcomeMessage();
            }
        }
    }

    // Show welcome message
    function showWelcomeMessage() {
        // Check if welcome message already shown or messages already exist
        if (hasInitialized && elements.messages.children.length > 0) {
            console.log('[Chatbox] Welcome message already shown, skipping');
            return;
        }
        
        const welcomeMessages = [
            "Xin chào! 👋 Tôi là trợ lý AI của Viet Home Service. Tôi có thể giúp bạn:",
            "🔍 Tìm kiếm dịch vụ",
            "📝 Đặt lịch dịch vụ",
            "💬 Trả lời câu hỏi về dịch vụ",
            "📞 Kết nối với nhân viên hỗ trợ"
        ];

        welcomeMessages.forEach((msg, index) => {
            setTimeout(() => {
                displayMessage(msg, 'AI', getVietnamTime());
            }, index * 200);
        });
        
        hasInitialized = true;
    }

    // Send message (using SignalR real-time)
    async function sendMessage() {
        const content = elements.input.value.trim();
        if (!content) return;

        console.log('[Chatbox] Sending message:', content);
        console.log('[Chatbox] SignalR connected:', isConnected);
        console.log('[Chatbox] ConversationId:', conversationId);

        // Display user message
        displayMessage(content, 'User', getVietnamTime());
        elements.input.value = '';
        elements.sendBtn.disabled = true;

        // Show typing indicator
        showTypingIndicator();

        try {
            // Create conversation if not exists
            if (!conversationId) {
                console.log('[Chatbox] Creating new conversation...');
                await createConversation();
                console.log('[Chatbox] Conversation created:', conversationId);
                
                // Join conversation via SignalR
                if (isConnected && conversationId) {
                    await connection.invoke('JoinConversation', conversationId);
                }
            }

            // Send message via SignalR if connected, otherwise fallback to HTTP
            if (isConnected && connection && conversationId) {
                console.log('[Chatbox] Sending via SignalR...');
                
                // Send typing indicator
                await connection.invoke('SendTypingIndicator', conversationId, true);
                
                // Send message
                await connection.invoke('SendMessage', conversationId, content, 'Text');
                
                // Hide typing indicator after a delay
                setTimeout(() => {
                    if (connection && isConnected) {
                        connection.invoke('SendTypingIndicator', conversationId, false);
                    }
                }, 1000);
                
            } else {
                // Fallback to HTTP API
                console.log('[Chatbox] SignalR not connected, using HTTP fallback...');
                await sendMessageHttp(content);
            }
        } catch (error) {
            console.error('[Chatbox] Error sending message:', error);
            hideTypingIndicator();
            
            // Fallback to HTTP if SignalR fails
            if (isConnected) {
                try {
                    await sendMessageHttp(content);
                } catch (httpError) {
                    displayMessage('Không thể kết nối đến server. Vui lòng thử lại sau.', 'AI', new Date());
                }
            } else {
                displayMessage('Không thể kết nối đến server. Vui lòng thử lại sau.', 'AI', new Date());
            }
        } finally {
            elements.sendBtn.disabled = false;
        }
    }

    // Fallback: Send message via HTTP API
    async function sendMessageHttp(content) {
        const headers = {
            'Content-Type': 'application/json'
        };
        
        if (jwt) {
            headers['Authorization'] = `Bearer ${jwt}`;
        }

        const response = await fetch(`${CONFIG.apiBaseUrl}/message`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({
                content: content,
                sessionId: CONFIG.sessionId,
                language: CONFIG.language
            })
        });

        if (response.ok) {
            const data = await response.json();
            hideTypingIndicator();
            
            const messageContent = data.Content || data.content || data.MessageContent || data.messageContent;
            
            if (messageContent) {
                displayMessage(messageContent, 'AI', new Date());
            } else {
                displayMessage('Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này. Vui lòng thử lại sau.', 'AI', new Date());
            }

            const quickActions = data.QuickActions || data.quickActions;
            if (quickActions && quickActions.length > 0) {
                showQuickActions(quickActions);
            }
        } else {
            const errorText = await response.text();
            console.error('[Chatbox] HTTP Error response:', response.status, errorText);
            hideTypingIndicator();
            displayMessage('Xin lỗi, đã xảy ra lỗi (' + response.status + '). Vui lòng thử lại.', 'AI', new Date());
        }
    }

    // Create conversation
    async function createConversation() {
        try {
            const headers = {
                'Content-Type': 'application/json'
            };
            
            if (jwt) {
                headers['Authorization'] = `Bearer ${jwt}`;
            }
            
            const response = await fetch(`${CONFIG.apiBaseUrl}/conversation`, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    content: content,
                    sessionId: CONFIG.sessionId,
                    language: CONFIG.language
                })
            });

            console.log('[Chatbox] Response status:', response.status);

            if (response.ok) {
                const data = await response.json();
                console.log('[Chatbox] Response data:', data);
                hideTypingIndicator();
                
                // Display AI response (check both uppercase and lowercase field names)
                const messageContent = data.Content || data.content || data.MessageContent || data.messageContent;
                
                if (messageContent) {
                    displayMessage(messageContent, 'AI', getVietnamTime());
                } else {
                    displayMessage('Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này. Vui lòng thử lại sau.', 'AI', getVietnamTime());
                    console.warn('[Chatbox] No message content found in response:', data);
                }

                // Show quick actions if available
                const quickActions = data.QuickActions || data.quickActions;
                if (quickActions && quickActions.length > 0) {
                    showQuickActions(quickActions);
                }
            } else {
                const errorText = await response.text();
                console.error('[Chatbox] Error response:', response.status, errorText);
                hideTypingIndicator();
                displayMessage('Xin lỗi, đã xảy ra lỗi (' + response.status + '). Vui lòng thử lại.', 'AI', getVietnamTime());
            }
        } catch (error) {
            console.error('[Chatbox] Error sending message:', error);
            hideTypingIndicator();
            displayMessage('Không thể kết nối đến server. Vui lòng thử lại sau.', 'AI', getVietnamTime());
        } finally {
            elements.sendBtn.disabled = false;
        }
    }

    // Create conversation
    async function createConversation() {
        try {
            const response = await fetch(`${CONFIG.apiBaseUrl}/conversation`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${jwt}`
                },
                body: JSON.stringify({
                    sessionId: CONFIG.sessionId,
                    language: CONFIG.language
                })
            });

            if (response.ok) {
                const data = await response.json();
                conversationId = data.conversationId || data.ConversationId;
                
                // Join conversation via SignalR if connected
                if (isConnected && connection && conversationId) {
                    await connection.invoke('JoinConversation', conversationId);
                }
            }
        } catch (error) {
            console.error('Error creating conversation:', error);
        }
    }

    // Display message
    function displayMessage(content, senderType, timestamp) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbox-message ${senderType.toLowerCase()}`;
        
        const time = formatTime(timestamp);
        
        // Format content for AI messages (handle markdown-like formatting)
        let formattedContent = content;
        if (senderType === 'AI') {
            formattedContent = formatAIMessage(content);
        } else {
            formattedContent = escapeHtml(content);
        }
        
        messageDiv.innerHTML = `
            <div class="chatbox-avatar">${senderType === 'User' ? '👤' : '🤖'}</div>
            <div class="chatbox-message-content">
                <div class="chatbox-message-text">${formattedContent}</div>
                <div class="chatbox-message-time">${time}</div>
            </div>
        `;

        elements.messages.appendChild(messageDiv);
        scrollToBottom();
    }

    // Format AI message with markdown-like syntax
    function formatAIMessage(text) {
        let formatted = escapeHtml(text);
        
        // Convert line breaks to <br>
        formatted = formatted.replace(/\n/g, '<br>');
        
        // Convert bullet points (• or -) to styled lists
        formatted = formatted.replace(/^[•\-]\s+(.+)$/gm, '<div class="ai-bullet">• $1</div>');
        
        // Convert numbered lists
        formatted = formatted.replace(/^(\d+)\.\s+(.+)$/gm, '<div class="ai-numbered"><span class="ai-number">$1.</span> $2</div>');
        
        // Convert headers (##)
        formatted = formatted.replace(/^##\s+(.+)$/gm, '<div class="ai-heading">$1</div>');
        
        // Convert bold text (**text**)
        formatted = formatted.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        
        // Convert code blocks (`code`)
        formatted = formatted.replace(/`(.+?)`/g, '<code class="ai-code">$1</code>');
        
        return formatted;
    }

    // Show typing indicator
    function showTypingIndicator() {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'chatbox-message ai';
        typingDiv.id = 'typingIndicator';
        typingDiv.innerHTML = `
            <div class="chatbox-avatar">🤖</div>
            <div class="chatbox-message-content">
                <div class="chatbox-typing">
                    <div class="chatbox-typing-dot"></div>
                    <div class="chatbox-typing-dot"></div>
                    <div class="chatbox-typing-dot"></div>
                </div>
            </div>
        `;
        elements.messages.appendChild(typingDiv);
        scrollToBottom();
    }

    // Hide typing indicator
    function hideTypingIndicator() {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) {
            indicator.remove();
        }
    }

    // Show quick actions
    function showQuickActions(actions) {
        elements.quickActions.innerHTML = '';
        
        if (!actions || actions.length === 0) return;
        
        actions.forEach(action => {
            const btn = document.createElement('button');
            btn.className = 'chatbox-quick-btn';
            
            // Add icon based on action type
            const icon = getActionIcon(action.Action || action.action);
            btn.innerHTML = `${icon} <span>${action.Title || action.title || action.label}</span>`;
            
            btn.addEventListener('click', () => {
                handleQuickAction(action);
            });
            elements.quickActions.appendChild(btn);
        });
    }

    // Get icon for action type
    function getActionIcon(actionType) {
        const icons = {
            'view_services': '🔍',
            'find_services': '🔍',
            'view_pricing': '💰',
            'view_vouchers': '🎟️',
            'book_service': '📅',
            'book_appointment': '📅',
            'ask_more': '💬',
            'contact_support': '📞'
        };
        return icons[actionType] || '▶️';
    }

    // Handle quick action click
    function handleQuickAction(action) {
        const actionType = action.Action || action.action;
        const actionTitle = action.Title || action.title || action.label;
        
        // Map actions to actual URLs or messages
        switch(actionType) {
            case 'view_services':
            case 'find_services':
                window.location.href = '/Customer/ServiceCustomer/ServiceCustomer';
                break;
            case 'view_pricing':
                elements.input.value = 'Cho tôi xem bảng giá các dịch vụ';
                sendMessage();
                break;
            case 'view_vouchers':
                window.location.href = '/Customer/Voucher/Voucher';
                break;
            case 'book_service':
            case 'book_appointment':
                window.location.href = '/Customer/ServiceCustomer/ServiceCustomer';
                break;
            default:
                elements.input.value = actionTitle;
                sendMessage();
        }
    }

    // Scroll to bottom
    function scrollToBottom() {
        elements.messages.scrollTop = elements.messages.scrollHeight;
    }

    // Get Vietnam time (UTC+7)
    function getVietnamTime(date) {
        if (!date) {
            // Get current time in Vietnam timezone
            const now = new Date();
            const vietnamTimeString = now.toLocaleString('en-US', { timeZone: 'Asia/Ho_Chi_Minh' });
            return new Date(vietnamTimeString);
        }
        if (typeof date === 'string') {
            date = new Date(date);
        }
        // Convert to Vietnam time (UTC+7)
        // Get the time string in Vietnam timezone
        const vietnamTimeString = date.toLocaleString('en-US', { 
            timeZone: 'Asia/Ho_Chi_Minh',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });
        return new Date(vietnamTimeString);
    }
    
    // Format time (Vietnam timezone)
    function formatTime(date) {
        if (typeof date === 'string') {
            date = new Date(date);
        }
        
        // Get current Vietnam time
        const now = getVietnamTime();
        const messageTime = getVietnamTime(date);
        
        const diffMs = now - messageTime;
        const diffMins = Math.floor(diffMs / 60000);

        if (diffMins < 1) return 'Vừa xong';
        if (diffMins < 60) return `${diffMins} phút trước`;
        if (diffMins < 1440) return `${Math.floor(diffMins / 60)} giờ trước`;
        
        // Format date in Vietnam locale
        return messageTime.toLocaleDateString('vi-VN', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    // Escape HTML
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        console.log('[Chatbox] DOM is loading, waiting for DOMContentLoaded');
        document.addEventListener('DOMContentLoaded', init);
    } else {
        console.log('[Chatbox] DOM already ready, initializing...');
        init();
    }
    
    console.log('[Chatbox] Module loaded');

})();

