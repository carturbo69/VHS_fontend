// Translation system - Dịch từ tiếng Việt sang tiếng Anh
(function() {
    'use strict';
    
    const TRANSLATE_STORAGE_KEY = 'vhs-translate-enabled';
    const TRANSLATE_CACHE_KEY = 'vhs-translate-cache';
    const TRANSLATE_API_URL = 'https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=en&dt=t&q=';
    
    let isTranslated = false;
    let originalTexts = new Map();
    let translationCache = new Map();
    
    // Load cache từ localStorage
    function loadCache() {
        try {
            const cached = localStorage.getItem(TRANSLATE_CACHE_KEY);
            if (cached) {
                const parsed = JSON.parse(cached);
                translationCache = new Map(Object.entries(parsed));
            }
        } catch (e) {
            console.warn('Không thể load translation cache:', e);
        }
    }
    
    // Lưu cache vào localStorage
    function saveCache() {
        try {
            const obj = Object.fromEntries(translationCache);
            localStorage.setItem(TRANSLATE_CACHE_KEY, JSON.stringify(obj));
        } catch (e) {
            console.warn('Không thể lưu translation cache:', e);
        }
    }
    
    // Khởi tạo
    function init() {
        loadCache();
        
        // Kiểm tra trạng thái đã lưu
        const saved = localStorage.getItem(TRANSLATE_STORAGE_KEY);
        if (saved === 'true') {
            isTranslated = true;
            // Đợi DOM load xong rồi mới dịch (không delay để nhanh nhất)
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => {
                    // Dịch ngay lập tức
                    translatePage();
                });
            } else {
                // Dịch ngay lập tức
                translatePage();
            }
        }
        
        // Tạo nút dịch nếu chưa có (giảm delay)
        setTimeout(createTranslateButton, 200);
        
        // Thiết lập MutationObserver để dịch các phần tử được load động (AJAX, PartialView, v.v.)
        setupDynamicTranslationObserver();
    }
    
    // Thiết lập observer để dịch các phần tử được thêm vào DOM động
    function setupDynamicTranslationObserver() {
        // Chỉ thiết lập nếu đang ở chế độ dịch
        const saved = localStorage.getItem(TRANSLATE_STORAGE_KEY);
        if (saved !== 'true') {
            return;
        }
        
        // Observer để theo dõi các phần tử mới được thêm vào
        const observer = new MutationObserver((mutations) => {
            if (!isTranslated) return;
            
            const newElements = [];
            
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    // Chỉ xử lý element nodes
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        // Bỏ qua script, style
                        if (node.tagName === 'SCRIPT' || node.tagName === 'STYLE') {
                            return;
                        }
                        
                        // Kiểm tra xem có phần tử cần dịch không
                        const translatableElements = node.querySelectorAll ? 
                            Array.from(node.querySelectorAll('*')).filter(el => {
                                if (shouldSkipElement(el)) return false;
                                const text = getPureText(el);
                                return text && text.trim().length > 0 && containsVietnamese(text);
                            }) : [];
                        
                        // Nếu chính node đó cần dịch
                        let nodeText = '';
                        if (!shouldSkipElement(node)) {
                            nodeText = getPureText(node);
                            if (nodeText && nodeText.trim().length > 0 && containsVietnamese(nodeText)) {
                                newElements.push(node);
                            }
                        }
                        
                        // Thêm các child elements cần dịch
                        translatableElements.forEach(el => {
                            if (!el.hasAttribute('data-translated')) {
                                newElements.push(el);
                            }
                        });
                    }
                });
            });
            
            // Dịch các phần tử mới nếu có (không delay để nhanh nhất)
            if (newElements.length > 0) {
                // Sử dụng requestAnimationFrame để không block UI nhưng không delay
                requestAnimationFrame(() => {
                    translateNewElements(newElements);
                });
            }
        });
        
        // Bắt đầu quan sát body và các thay đổi
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
        
        // Lưu observer để có thể dừng lại nếu cần
        window.translateObserver = observer;
    }
    
    // Dịch các phần tử mới được thêm vào
    async function translateNewElements(elements) {
        if (elements.length === 0 || !isTranslated) return;
        
        // Lọc các phần tử chưa được dịch và có text tiếng Việt
        const elementsToTranslate = elements.filter(el => {
            if (el.hasAttribute('data-translated')) return false;
            if (shouldSkipElement(el)) return false;
            const text = getPureText(el);
            return text && text.trim().length > 0 && containsVietnamese(text);
        });
        
        if (elementsToTranslate.length === 0) return;
        
        // Nhóm các text cần dịch
        const textsToTranslate = new Map();
        const elementsWithCache = [];
        
        elementsToTranslate.forEach(el => {
            const text = getPureText(el);
            if (text && text.trim().length > 0) {
                // Kiểm tra cache trước
                if (translationCache.has(text)) {
                    elementsWithCache.push({ el, text });
                } else {
                    if (!textsToTranslate.has(text)) {
                        textsToTranslate.set(text, []);
                    }
                    textsToTranslate.get(text).push(el);
                }
            }
        });
        
        // Xử lý cache trước (ngay lập tức)
        elementsWithCache.forEach(({ el, text }) => {
            const translated = translationCache.get(text);
            if (translated) {
                applyTranslation(el, translated);
            }
        });
        
        // Dịch các text chưa có trong cache (không delay để nhanh nhất)
        const uniqueTexts = Array.from(textsToTranslate.keys());
        if (uniqueTexts.length > 0) {
            // Dịch tất cả cùng lúc, không batch để nhanh nhất
            await Promise.all(uniqueTexts.map(async (text) => {
                try {
                    const translated = await translateText(text);
                    const elements = textsToTranslate.get(text);
                    if (elements && translated && translated !== text) {
                        elements.forEach(el => {
                            applyTranslation(el, translated);
                        });
                        // Lưu vào cache
                        translationCache.set(text, translated);
                    }
                } catch (error) {
                    console.warn('Lỗi khi dịch phần tử mới:', error);
                }
            }));
            
            // Lưu cache
            saveCache();
        }
    }
    
    // Tạo nút dịch
    function createTranslateButton() {
        // Kiểm tra xem nút đã tồn tại chưa
        if (document.getElementById('translateBtn')) {
            return;
        }
        
        // Tìm vị trí để chèn nút (gần nút theme toggle)
        const themeBtn = document.querySelector('.icon-btn[onclick*="__toggleTheme"]');
        const navActions = document.querySelector('.nav-actions'); // Customer layout
        const topbarRight = document.querySelector('.topbar .right'); // Provider & Admin layout
        
        // Nếu không có nav-actions và không có topbar.right (trang auth), không tạo nút (chỉ tự động dịch nếu đã bật)
        if (!navActions && !topbarRight) {
            return; // Không tạo nút ở trang auth, chỉ tự động dịch nếu đã bật
        }
        
        // Tạo nút dịch
        const translateBtn = document.createElement('button');
        translateBtn.id = 'translateBtn';
        translateBtn.className = 'icon-btn translate-btn';
        translateBtn.setAttribute('aria-label', 'Dịch sang tiếng Anh');
        translateBtn.innerHTML = '<i class="bi bi-translate"></i>';
        translateBtn.title = isTranslated ? 'Chuyển về tiếng Việt' : 'Dịch sang tiếng Anh';
        
        // Thêm style cho nút
        translateBtn.style.cssText = `
            position: relative;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 40px;
            height: 40px;
            border-radius: 8px;
            border: none;
            background: transparent;
            color: inherit;
            cursor: pointer;
            transition: all 0.2s ease;
        `;
        
        // Hover effect
        translateBtn.addEventListener('mouseenter', function() {
            this.style.background = 'rgba(0, 0, 0, 0.05)';
        });
        translateBtn.addEventListener('mouseleave', function() {
            this.style.background = 'transparent';
        });
        
        // Event listener
        translateBtn.addEventListener('click', toggleTranslate);
        
        // Chèn nút vào DOM
        if (navActions) {
            // Customer layout - chèn vào nav-actions
            if (themeBtn && themeBtn.parentNode === navActions) {
                navActions.insertBefore(translateBtn, themeBtn);
            } else {
                navActions.insertBefore(translateBtn, navActions.firstChild);
            }
        } else if (topbarRight) {
            // Provider layout - chèn vào topbar.right, trước nút theme
            if (themeBtn && themeBtn.parentNode === topbarRight) {
                topbarRight.insertBefore(translateBtn, themeBtn);
            } else {
                // Nếu không tìm thấy themeBtn, chèn vào trước user section
                const userSection = topbarRight.querySelector('.user');
                if (userSection) {
                    topbarRight.insertBefore(translateBtn, userSection);
                } else {
                    topbarRight.insertBefore(translateBtn, topbarRight.firstChild);
                }
            }
        }
        
        // Cập nhật trạng thái nút
        updateButtonState();
    }
    
    // Cập nhật trạng thái nút
    function updateButtonState() {
        const btn = document.getElementById('translateBtn');
        if (!btn) return;
        
        if (isTranslated) {
            btn.innerHTML = '<i class="bi bi-translate" style="color: #10b981;"></i>';
            btn.title = 'Chuyển về tiếng Việt';
            btn.setAttribute('aria-label', 'Chuyển về tiếng Việt');
        } else {
            btn.innerHTML = '<i class="bi bi-translate"></i>';
            btn.title = 'Dịch sang tiếng Anh';
            btn.setAttribute('aria-label', 'Dịch sang tiếng Anh');
        }
    }
    
    // Toggle dịch
    async function toggleTranslate() {
        const btn = document.getElementById('translateBtn');
        if (btn) {
            btn.disabled = true;
            btn.style.opacity = '0.6';
        }
        
        if (isTranslated) {
            restoreOriginalTexts();
            isTranslated = false;
            localStorage.setItem(TRANSLATE_STORAGE_KEY, 'false');
            // Dừng observer khi tắt dịch
            if (window.translateObserver) {
                window.translateObserver.disconnect();
                window.translateObserver = null;
            }
        } else {
            await translatePage();
            isTranslated = true;
            localStorage.setItem(TRANSLATE_STORAGE_KEY, 'true');
            // Thiết lập observer nếu chưa có
            if (!window.translateObserver) {
                setupDynamicTranslationObserver();
            }
        }
        
        updateButtonState();
        if (btn) {
            btn.disabled = false;
            btn.style.opacity = '1';
        }
    }
    
    // Dịch toàn bộ trang
    async function translatePage() {
        // Lưu text gốc nếu chưa có
        if (originalTexts.size === 0) {
            saveOriginalTexts();
        }
        
        // Lấy tất cả các phần tử cần dịch
        const elements = getTranslatableElements();
        
        if (elements.length === 0) {
            return;
        }
        
        // Sắp xếp elements theo độ ưu tiên: phần tử hiển thị trước (above the fold) được dịch trước
        const visibleElements = [];
        const hiddenElements = [];
        
        elements.forEach(el => {
            const rect = el.getBoundingClientRect();
            const isVisible = rect.top < window.innerHeight * 2; // Trong viewport hoặc gần viewport
            if (isVisible) {
                visibleElements.push(el);
            } else {
                hiddenElements.push(el);
            }
        });
        
        // Dịch phần tử hiển thị trước
        const allElements = [...visibleElements, ...hiddenElements];
        
        // Hiển thị loading indicator
        showLoadingIndicator();
        
        // Nhóm các text cần dịch để batch translate
        const textsToTranslate = new Map();
        const elementsWithCache = [];
        
        allElements.forEach(el => {
            // Lấy text thuần túy, không bao gồm HTML
            const text = getPureText(el);
            if (text && text.trim().length > 0) {
                // Kiểm tra cache trước
                if (translationCache.has(text)) {
                    elementsWithCache.push({ el, text });
                } else {
                    // Chưa có trong cache, cần dịch
                    if (!textsToTranslate.has(text)) {
                        textsToTranslate.set(text, []);
                    }
                    textsToTranslate.get(text).push(el);
                }
            }
        });
        
        // Xử lý các element đã có trong cache trước (nhanh, không cần API) - ngay lập tức
        elementsWithCache.forEach(({ el, text }) => {
            const translated = translationCache.get(text);
            if (translated) {
                applyTranslation(el, translated);
            }
        });
        
        // Dịch từng nhóm text (mỗi text có thể xuất hiện ở nhiều element)
        const uniqueTexts = Array.from(textsToTranslate.keys());
        
        // Sắp xếp text theo độ ưu tiên: text ngắn trước (dịch nhanh hơn), text dài sau
        uniqueTexts.sort((a, b) => a.length - b.length);
        
        // Dịch tất cả cùng lúc, không delay để nhanh nhất
        await Promise.all(uniqueTexts.map(async (text) => {
            try {
                const translated = await translateText(text);
                // Áp dụng cho tất cả elements có cùng text
                const elements = textsToTranslate.get(text);
                if (elements && translated && translated !== text) {
                    elements.forEach(el => {
                        applyTranslation(el, translated);
                    });
                }
            } catch (error) {
                console.warn('Lỗi khi dịch:', error);
            }
        }));
        
        // Ẩn loading indicator
        hideLoadingIndicator();
        
        // Lưu cache
        saveCache();
    }
    
    // Lấy text thuần túy từ element (chỉ lấy text nodes, giữ nguyên format)
    function getPureText(element) {
        // Đặc biệt xử lý button và link - lấy textContent trực tiếp
        if (element.tagName === 'BUTTON' || element.tagName === 'A') {
            // Lấy text từ text nodes, bỏ qua icon
            let text = '';
            for (const node of element.childNodes) {
                if (node.nodeType === Node.TEXT_NODE) {
                    text += node.textContent;
                }
            }
            return text.trim();
        }
        
        // Nếu element không có child nodes, lấy textContent nhưng giữ nguyên khoảng trắng
        if (element.childNodes.length === 0) {
            return element.textContent; // Không trim để giữ format
        }
        
        // Nếu có child nodes, chỉ lấy text từ text nodes, giữ nguyên khoảng trắng và ký tự đặc biệt
        let text = '';
        for (const node of element.childNodes) {
            if (node.nodeType === Node.TEXT_NODE) {
                // Giữ nguyên text node, không trim để giữ format (bullet points, line breaks trong text)
                const nodeText = node.textContent;
                text += nodeText;
            }
        }
        // Chỉ trim đầu/cuối, giữ nguyên khoảng trắng giữa các từ
        return text.trim();
    }
    
    // Áp dụng bản dịch vào element một cách an toàn (giữ nguyên toàn bộ HTML structure và format)
    function applyTranslation(element, translatedText) {
        try {
            // Đặc biệt xử lý button và link - luôn dịch nếu có text (giống như "Forgot password?")
            if (element.tagName === 'BUTTON' || element.tagName === 'A') {
                // Lưu tất cả child elements (icon, span, v.v.) để giữ lại
                const childElements = Array.from(element.childNodes).filter(node => 
                    node.nodeType === Node.ELEMENT_NODE
                );
                
                // Nếu có child elements (icon, v.v.), giữ lại và chỉ thay text
                if (childElements.length > 0) {
                    // Lưu HTML của các child elements
                    const childHTML = childElements.map(el => el.outerHTML).join('');
                    
                    // Xác định vị trí text (trước hoặc sau child elements) dựa trên thứ tự trong DOM
                    const allNodes = Array.from(element.childNodes);
                    const firstChildIndex = allNodes.indexOf(childElements[0]);
                    const textNodes = allNodes.filter(node => 
                        node.nodeType === Node.TEXT_NODE && node.textContent.trim().length > 0
                    );
                    const firstTextIndex = textNodes.length > 0 ? allNodes.indexOf(textNodes[0]) : -1;
                    
                    // Thay thế: giữ nguyên child elements, chỉ thay text
                    if (firstTextIndex >= 0 && firstTextIndex < firstChildIndex) {
                        // Text ở trước child elements
                        element.innerHTML = translatedText + ' ' + childHTML;
                    } else {
                        // Text ở sau child elements hoặc không có text node
                        element.innerHTML = childHTML + ' ' + translatedText;
                    }
                } else {
                    // Không có child elements, thay textContent trực tiếp (giống "Forgot password?")
                    element.textContent = translatedText;
                }
                element.setAttribute('data-translated', 'true');
                return;
            }
            
            // Nếu element không có child nodes, chỉ cần thay textContent
            if (element.childNodes.length === 0) {
                element.textContent = translatedText;
                element.setAttribute('data-translated', 'true');
                return;
            }
            
            // Nếu có child nodes, chỉ thay text nodes, giữ nguyên toàn bộ HTML structure
            const textNodes = [];
            const elementNodes = [];
            
            // Phân loại nodes
            for (const node of element.childNodes) {
                if (node.nodeType === Node.TEXT_NODE) {
                    textNodes.push(node);
                } else {
                    elementNodes.push(node);
                }
            }
            
            if (textNodes.length > 0) {
                // Giữ nguyên khoảng trắng đầu/cuối và format
                const firstNodeText = textNodes[0].textContent;
                const lastNodeText = textNodes[textNodes.length - 1].textContent;
                const leadingWhitespace = firstNodeText.match(/^\s*/)?.[0] || '';
                const trailingWhitespace = lastNodeText.match(/\s*$/)?.[0] || '';
                
                // Thay thế text node đầu tiên bằng bản dịch, giữ nguyên format
                if (textNodes[0]) {
                    // Giữ nguyên khoảng trắng và các ký tự đặc biệt (bullet points, line breaks trong text)
                    textNodes[0].textContent = leadingWhitespace + translatedText + trailingWhitespace;
                    
                    // Xóa các text node còn lại để tránh duplicate
                    for (let i = 1; i < textNodes.length; i++) {
                        if (textNodes[i].parentNode) {
                            textNodes[i].parentNode.removeChild(textNodes[i]);
                        }
                    }
                }
                element.setAttribute('data-translated', 'true');
            } else if (elementNodes.length > 0) {
                // Nếu không có text node nhưng có child elements
                // Thêm text node mới vào đầu (trước child đầu tiên)
                const textNode = document.createTextNode(translatedText + ' ');
                element.insertBefore(textNode, elementNodes[0]);
                element.setAttribute('data-translated', 'true');
            } else {
                // Fallback: thêm text node vào đầu
                const textNode = document.createTextNode(translatedText);
                element.insertBefore(textNode, element.firstChild);
                element.setAttribute('data-translated', 'true');
            }
        } catch (error) {
            console.warn('Lỗi khi áp dụng bản dịch:', error);
            // Fallback: thay textContent cho button/link
            if (element.tagName === 'BUTTON' || element.tagName === 'A') {
                element.textContent = translatedText;
            } else if (element.children.length === 0 || !element.querySelector('i, svg, img, button, a')) {
                element.textContent = translatedText;
            }
        }
    }
    
    // Lưu text gốc
    function saveOriginalTexts() {
        const elements = getTranslatableElements();
        elements.forEach(el => {
            const text = getPureText(el);
            if (text && text.trim().length > 0) {
                // Sử dụng một cách đơn giản hơn để lưu
                const key = el.getAttribute('data-translate-key') || generateKey(el);
                el.setAttribute('data-translate-key', key);
                originalTexts.set(key, {
                    text: text,
                    hasChildren: el.childNodes.length > 0,
                    html: el.innerHTML // Lưu HTML để restore chính xác
                });
            }
        });
    }
    
    // Tạo key cho element
    function generateKey(element) {
        return `el_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    // Khôi phục text gốc
    function restoreOriginalTexts() {
        originalTexts.forEach((data, key) => {
            const element = document.querySelector(`[data-translate-key="${key}"]`);
            if (element) {
                if (data.html) {
                    // Restore HTML nếu đã lưu
                    element.innerHTML = data.html;
                } else {
                    // Fallback: chỉ restore text
                    element.textContent = data.text;
                }
                element.removeAttribute('data-translated');
            }
        });
    }
    
    // Lấy tất cả các phần tử cần dịch
    function getTranslatableElements() {
        const elements = [];
        const seen = new Set();
        
        // Duyệt qua tất cả các phần tử trong body
        const allElements = document.querySelectorAll('body *');
        
        allElements.forEach(el => {
            // Bỏ qua nếu đã xử lý
            if (seen.has(el)) {
                return;
            }
            
            // Chỉ bỏ qua các phần tử thực sự không nên dịch
            if (shouldSkipElement(el)) {
                return;
            }
            
            // Lấy text thuần túy (chỉ text nodes, không bao gồm HTML)
            const text = getPureText(el);
            if (text && text.trim().length > 0) {
                // Chỉ dịch text tiếng Việt (có dấu)
                if (containsVietnamese(text)) {
                    // Dịch tất cả text có ít nhất 1 ký tự tiếng Việt
                    elements.push(el);
                    seen.add(el);
                    
                    // Đánh dấu các child elements để tránh dịch trùng
                    // Nhưng vẫn cho phép dịch text trong child nếu cần
                    el.querySelectorAll('*').forEach(child => {
                        // Đặc biệt: button, link, và heading tags (H1-H6) luôn được dịch riêng (không đánh dấu)
                        if (child.tagName === 'BUTTON' || 
                            child.tagName === 'A' || 
                            /^H[1-6]$/.test(child.tagName)) {
                            // Không đánh dấu button/link/heading để chúng được dịch riêng
                            return;
                        }
                        
                        // Chỉ đánh dấu nếu child không có text riêng
                        const childText = getPureText(child);
                        if (!childText || childText.trim().length === 0) {
                            seen.add(child);
                        } else {
                            // Nếu child có text riêng và có tiếng Việt, vẫn dịch riêng
                            if (containsVietnamese(childText)) {
                                // Không đánh dấu để child được dịch riêng
                                return;
                            }
                            // Nếu child không có tiếng Việt, đánh dấu để tránh dịch trùng
                            const inlineTags = ['STRONG', 'EM', 'B', 'I', 'SPAN', 'SMALL', 'SUB', 'SUP', 'MARK'];
                            if (!inlineTags.includes(child.tagName)) {
                                seen.add(child);
                            }
                        }
                    });
                }
            }
        });
        
        return elements;
    }
    
    // Kiểm tra xem có nên bỏ qua element không (chỉ bỏ qua những phần tử thực sự không nên dịch)
    function shouldSkipElement(element) {
        // Bỏ qua script, style, noscript, template
        if (element.tagName === 'SCRIPT' || 
            element.tagName === 'STYLE' || 
            element.tagName === 'NOSCRIPT' ||
            element.tagName === 'TEMPLATE') {
            return true;
        }
        
        // Bỏ qua các phần tử trong SVG (giữ nguyên SVG)
        if (element.closest('svg') || element.tagName === 'SVG') {
            return true;
        }
        
        // Bỏ qua image, video, audio
        if (element.tagName === 'IMG' || 
            element.tagName === 'VIDEO' || 
            element.tagName === 'AUDIO' ||
            element.tagName === 'CANVAS' ||
            element.tagName === 'IFRAME') {
            return true;
        }
        
        // Bỏ qua các phần tử có class đặc biệt (nếu người dùng đánh dấu không dịch)
        if (element.classList.contains('no-translate') || element.hasAttribute('data-no-translate')) {
            return true;
        }
        
        // Bỏ qua các phần tử chỉ chứa số thuần túy (không có chữ)
        const text = getPureText(element);
        if (text && /^[\d\s\-\+\*\/\(\)\.\,\:\;]+$/.test(text.trim())) {
            return true;
        }
        
        // Bỏ qua các phần tử có ID đặc biệt (chỉ nút dịch)
        if (element.id === 'translateBtn') {
            return true;
        }
        
        // Bỏ qua các phần tử chỉ chứa icon thuần túy (không có text)
        // Chỉ bỏ qua nếu KHÔNG có text node nào
        const textNodes = Array.from(element.childNodes).filter(n => n.nodeType === Node.TEXT_NODE);
        const hasText = textNodes.some(n => n.textContent.trim().length > 0);
        if (!hasText && element.querySelector('i, svg, .bi') && element.children.length > 0) {
            // Chỉ bỏ qua nếu hoàn toàn không có text và chỉ có icon
            return true;
        }
        
        // Cho phép dịch button, input[type="button"], input[type="submit"], link, và heading tags (H1-H6) nếu có text
        // ĐẶC BIỆT: Button, link, và heading tags luôn được dịch nếu có text (không bỏ qua)
        if (element.tagName === 'BUTTON' || 
            element.tagName === 'A' ||
            /^H[1-6]$/.test(element.tagName) ||
            (element.tagName === 'INPUT' && (element.type === 'button' || element.type === 'submit'))) {
            // Lấy text từ button/link/heading (chỉ text nodes, không bao gồm icon)
            const text = getPureText(element);
            if (text && text.trim().length > 0) {
                return false; // Cho phép dịch button/link/heading có text
            }
            // Nếu không có text, vẫn bỏ qua
            return true;
        }
        
        // Bỏ qua các phần tử ẩn hoàn toàn (không hiển thị) - nhưng vẫn dịch button/link
        const computedStyle = window.getComputedStyle(element);
        if (computedStyle.display === 'none' || 
            computedStyle.visibility === 'hidden' ||
            computedStyle.opacity === '0') {
            // Nhưng vẫn dịch nếu có thể hiển thị sau này (như dropdown menu)
            if (element.hasAttribute('hidden') && !element.classList.contains('show')) {
                return false; // Vẫn dịch để khi hiển thị đã có bản dịch
            }
            return true; // Bỏ qua phần tử ẩn
        }
        
        return false;
    }
    
    // Kiểm tra xem text có chứa tiếng Việt không
    function containsVietnamese(text) {
        // Kiểm tra có ký tự tiếng Việt (có dấu)
        const vietnameseRegex = /[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐ]/i;
        return vietnameseRegex.test(text);
    }
    
    // Dịch một element
    async function translateElement(element) {
        const text = element.textContent.trim();
        if (!text || text.length === 0) {
            return;
        }
        
        try {
            // Kiểm tra cache trước
            if (translationCache.has(text)) {
                const cached = translationCache.get(text);
                element.textContent = cached;
                element.setAttribute('data-translated', 'true');
                return;
            }
            
            // Sử dụng Google Translate API
            const translated = await translateText(text);
            if (translated && translated !== text) {
                element.textContent = translated;
                element.setAttribute('data-translated', 'true');
                // Lưu vào cache
                translationCache.set(text, translated);
            }
        } catch (error) {
            console.warn('Lỗi khi dịch:', error);
        }
    }
    
    // Hiển thị loading indicator
    function showLoadingIndicator() {
        const btn = document.getElementById('translateBtn');
        if (btn) {
            btn.style.opacity = '0.6';
            btn.style.pointerEvents = 'none';
            btn.innerHTML = '<i class="bi bi-hourglass-split"></i>';
        }
    }
    
    // Ẩn loading indicator
    function hideLoadingIndicator() {
        updateButtonState();
        const btn = document.getElementById('translateBtn');
        if (btn) {
            btn.style.opacity = '1';
            btn.style.pointerEvents = 'auto';
        }
    }
    
    // Dịch text sử dụng Google Translate API
    async function translateText(text) {
        if (!text || text.trim().length === 0) {
            return text;
        }
        
        // Kiểm tra cache
        const cacheKey = text.trim();
        if (translationCache.has(cacheKey)) {
            return translationCache.get(cacheKey);
        }
        
        try {
            // Giới hạn độ dài để tránh lỗi
            const maxLength = 5000;
            if (text.length > maxLength) {
                // Chia nhỏ text nếu quá dài
                const chunks = text.match(new RegExp(`.{1,${maxLength}}`, 'g')) || [text];
                const translatedChunks = await Promise.all(
                    chunks.map(chunk => translateTextChunk(chunk))
                );
                const result = translatedChunks.join(' ');
                // Lưu vào cache
                translationCache.set(cacheKey, result);
                return result;
            }
            
            const result = await translateTextChunk(text);
            // Lưu vào cache
            if (result && result !== text) {
                translationCache.set(cacheKey, result);
            }
            return result;
        } catch (error) {
            console.error('Lỗi khi dịch text:', error);
            return text;
        }
    }
    
    // Dịch một đoạn text
    async function translateTextChunk(text) {
        try {
            const encodedText = encodeURIComponent(text);
            const url = `${TRANSLATE_API_URL}${encodedText}`;
            
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            // Parse kết quả từ Google Translate API
            if (data && data[0] && Array.isArray(data[0])) {
                const translated = data[0]
                    .map(item => item[0])
                    .filter(item => item)
                    .join('');
                return translated || text;
            }
            
            return text;
        } catch (error) {
            console.error('Lỗi khi gọi Google Translate API:', error);
            return text;
        }
    }
    
    // Khởi tạo khi DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
    
    // Export function để có thể gọi từ bên ngoài
    window.VHSTranslate = {
        toggle: toggleTranslate,
        translate: translatePage,
        restore: restoreOriginalTexts,
        isTranslated: () => isTranslated,
        clearCache: () => {
            translationCache.clear();
            localStorage.removeItem(TRANSLATE_CACHE_KEY);
        },
        translateElement: async (element) => {
            // Dịch một phần tử cụ thể (dùng cho AJAX content)
            if (!isTranslated) return;
            if (!element || !element.nodeType) return;
            
            // Lấy tất cả elements cần dịch (bao gồm cả element chính và tất cả child)
            const allElements = element.querySelectorAll ? 
                Array.from(element.querySelectorAll('*')) : [];
            
            // Thêm element chính vào đầu danh sách
            const elementsToTranslate = [element, ...allElements].filter(el => {
                if (shouldSkipElement(el)) return false;
                if (el.hasAttribute('data-translated')) return false;
                const text = getPureText(el);
                return text && text.trim().length > 0 && containsVietnamese(text);
            });
            
            if (elementsToTranslate.length === 0) return;
            
            // Nhóm các text cần dịch
            const textsToTranslate = new Map();
            const elementsWithCache = [];
            
            elementsToTranslate.forEach(el => {
                const text = getPureText(el);
                if (text && text.trim().length > 0) {
                    // Kiểm tra cache trước
                    if (translationCache.has(text)) {
                        elementsWithCache.push({ el, text });
                    } else {
                        if (!textsToTranslate.has(text)) {
                            textsToTranslate.set(text, []);
                        }
                        textsToTranslate.get(text).push(el);
                    }
                }
            });
            
            // Xử lý cache trước (ngay lập tức)
            elementsWithCache.forEach(({ el, text }) => {
                const translated = translationCache.get(text);
                if (translated) {
                    applyTranslation(el, translated);
                }
            });
            
            // Dịch các text chưa có trong cache
            const uniqueTexts = Array.from(textsToTranslate.keys());
            if (uniqueTexts.length > 0) {
                // Dịch tất cả cùng lúc
                await Promise.all(uniqueTexts.map(async (text) => {
                    try {
                        const translated = await translateText(text);
                        const elements = textsToTranslate.get(text);
                        if (elements && translated && translated !== text) {
                            elements.forEach(el => {
                                applyTranslation(el, translated);
                            });
                            // Lưu vào cache
                            translationCache.set(text, translated);
                        }
                    } catch (error) {
                        console.warn('Lỗi khi dịch phần tử:', error);
                    }
                }));
                
                // Lưu cache
                saveCache();
            }
        }
    };
    
})();

