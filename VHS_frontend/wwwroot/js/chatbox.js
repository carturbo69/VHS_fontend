// Chatbox AI JavaScript
console.log('=== chatbox.js LOADED ===');

(function() {
    'use strict';
    console.log('=== chatbox.js IIFE STARTED ===');

    // Configuration
    const CONFIG = {
        apiBaseUrl: 'http://localhost:5154/api/ChatboxAI',
        sessionId: localStorage.getItem('chatbox_session_id') || generateSessionId(),
        language: 'vi',
        conversationId: null
    };

    // Initialize
    let jwt = null;
    let conversationId = null;
    let isMinimized = false;

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
        minimizeBtn: document.getElementById('chatboxMinimize'),
        quickActions: document.getElementById('chatboxQuickActions')
    };

    // Initialize chatbox
    function init() {
        console.log('[Chatbox] Initializing...');
        console.log('[Chatbox] DOM Elements:', elements);
        
        // Check if elements exist
        if (!elements.toggle) {
            console.error('[Chatbox] Toggle button not found!');
            return;
        }
        
        // Get JWT token from session or cookie
        jwt = getJWTToken();
        console.log('[Chatbox] JWT:', jwt ? 'Found' : 'Not found');
        
        // Add event listeners
        elements.toggle.addEventListener('click', toggleChatbox);
        elements.closeBtn.addEventListener('click', closeChatbox);
        elements.minimizeBtn.addEventListener('click', minimizeChatbox);
        elements.sendBtn.addEventListener('click', sendMessage);
        elements.input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        console.log('[Chatbox] Event listeners attached');

        // Load conversation if user is authenticated
        if (jwt) {
            console.log('[Chatbox] User authenticated, loading conversation...');
            loadConversation();
        } else {
            console.log('[Chatbox] No JWT, showing welcome message');
            showWelcomeMessage();
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
        elements.container.classList.toggle('open');
        if (elements.container.classList.contains('open')) {
            loadConversation();
        }
    }

    // Close chatbox
    function closeChatbox() {
        elements.container.classList.remove('open');
        isMinimized = false;
    }

    // Minimize chatbox
    function minimizeChatbox() {
        isMinimized = !isMinimized;
        if (isMinimized) {
            elements.container.style.height = '60px';
        } else {
            elements.container.style.height = '500px';
        }
    }

    // Load conversation
    async function loadConversation() {
        try {
            const headers = { 'Content-Type': 'application/json' };
            if (jwt) headers['Authorization'] = `Bearer ${jwt}`;

            const response = await fetch(`${CONFIG.apiBaseUrl}/conversation/active?sessionId=${CONFIG.sessionId}`, {
                method: 'GET',
                headers: headers
            });

            if (response.ok) {
                const data = await response.json();
                if (data.conversationId) {
                    conversationId = data.conversationId;
                    loadMessageHistory(data.conversationId);
                } else {
                    showWelcomeMessage();
                }
            } else {
                showWelcomeMessage();
            }
        } catch (error) {
            console.error('Error loading conversation:', error);
            showWelcomeMessage();
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
                data.messages?.forEach(msg => {
                    displayMessage(msg.content, msg.senderType, msg.createdAt);
                });
            }
        } catch (error) {
            console.error('Error loading history:', error);
        }
    }

    // Show welcome message
    function showWelcomeMessage() {
        const welcomeMessages = [
            "Xin chào! 👋 Tôi là trợ lý AI của Viet Home Service. Tôi có thể giúp bạn:",
            "🔍 Tìm kiếm dịch vụ",
            "📝 Đặt lịch dịch vụ",
            "💬 Trả lời câu hỏi về dịch vụ",
            "📞 Kết nối với nhân viên hỗ trợ"
        ];

        welcomeMessages.forEach((msg, index) => {
            setTimeout(() => {
                displayMessage(msg, 'AI', new Date());
            }, index * 200);
        });
    }

    // Send message
    async function sendMessage() {
        const content = elements.input.value.trim();
        if (!content) return;

        console.log('[Chatbox] Sending message:', content);
        console.log('[Chatbox] JWT exists:', !!jwt);
        console.log('[Chatbox] ConversationId:', conversationId);

        // Display user message
        displayMessage(content, 'User', new Date());
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
            }

            // Send message
            const headers = {
                'Content-Type': 'application/json'
            };
            
            if (jwt) {
                headers['Authorization'] = `Bearer ${jwt}`;
            }

            console.log('[Chatbox] Sending POST to:', `${CONFIG.apiBaseUrl}/message`);

            const response = await fetch(`${CONFIG.apiBaseUrl}/message`, {
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
                    displayMessage(messageContent, 'AI', new Date());
                } else {
                    displayMessage('Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này. Vui lòng thử lại sau.', 'AI', new Date());
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
                displayMessage('Xin lỗi, đã xảy ra lỗi (' + response.status + '). Vui lòng thử lại.', 'AI', new Date());
            }
        } catch (error) {
            console.error('[Chatbox] Error sending message:', error);
            hideTypingIndicator();
            displayMessage('Không thể kết nối đến server. Vui lòng thử lại sau.', 'AI', new Date());
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
                conversationId = data.conversationId;
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

    // Format time
    function formatTime(date) {
        if (typeof date === 'string') {
            date = new Date(date);
        }
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);

        if (diffMins < 1) return 'Vừa xong';
        if (diffMins < 60) return `${diffMins} phút trước`;
        if (diffMins < 1440) return `${Math.floor(diffMins / 60)} giờ trước`;
        return date.toLocaleDateString('vi-VN');
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

