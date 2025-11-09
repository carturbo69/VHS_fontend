/**
 * Image Helper - Hàm helper để lấy URL ảnh đầy đủ từ path tương đối
 * Không cần hardcode URL như http://localhost:5154
 */

(function() {
    'use strict';

    // Lấy base URL từ meta tag hoặc config
    function getBaseUrl() {
        // Thử lấy từ meta tag nếu có (backend có thể inject)
        const metaApiUrl = document.querySelector('meta[name="api-base-url"]')?.getAttribute('content');
        if (metaApiUrl) {
            return metaApiUrl;
        }

        // Fallback: dùng relative path nếu cùng domain hoặc localhost cho development
        const isProduction = window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1';
        if (isProduction) {
            // Trong production, nếu frontend và backend cùng domain, dùng relative path
            return window.location.origin;
        }
        
        // Development: mặc định localhost:5154
        return 'http://localhost:5154';
    }

    /**
     * Lấy URL đầy đủ của ảnh từ path tương đối
     * @param {string} imagePath - Đường dẫn ảnh (ví dụ: /uploads/staff/xxx.jpg)
     * @param {string} baseUrl - Base URL (optional, nếu không có sẽ tự động lấy)
     * @returns {string} URL đầy đủ của ảnh
     * 
     * @example
     * getImageUrl('/uploads/staff/abc.jpg')
     * // Returns: 'http://localhost:5154/uploads/staff/abc.jpg'
     * 
     * getImageUrl('/uploads/staff/abc.jpg', 'https://api.example.com')
     * // Returns: 'https://api.example.com/uploads/staff/abc.jpg'
     * 
     * getImageUrl('http://example.com/image.jpg')
     * // Returns: 'http://example.com/image.jpg' (nếu đã là URL đầy đủ)
     */
    window.getImageUrl = function(imagePath, baseUrl = null) {
        // Nếu path rỗng hoặc null, trả về empty string
        if (!imagePath || imagePath.trim() === '') {
            return '';
        }

        // Nếu đã là URL đầy đủ (bắt đầu bằng http:// hoặc https://), trả về luôn
        if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
            return imagePath;
        }

        // Lấy base URL
        const apiBaseUrl = baseUrl || getBaseUrl();

        // Kết hợp base URL với path (đảm bảo không có double slash)
        const normalizedPath = imagePath.startsWith('/') ? imagePath : '/' + imagePath;
        return `${apiBaseUrl.replace(/\/$/, '')}${normalizedPath}`;
    };

    /**
     * Set base URL cho image helper (nếu muốn override)
     * @param {string} url - Base URL mới
     */
    window.setImageBaseUrl = function(url) {
        if (url && typeof url === 'string') {
            // Lưu vào localStorage để dùng sau
            localStorage.setItem('imageBaseUrl', url);
        }
    };

    // Thử load base URL từ localStorage nếu có
    const savedBaseUrl = localStorage.getItem('imageBaseUrl');
    if (savedBaseUrl) {
        // Override getBaseUrl function nếu có saved URL
        const originalGetBaseUrl = getBaseUrl;
        getBaseUrl = function() {
            return savedBaseUrl;
        };
    }

    console.log('[ImageHelper] Loaded - Use getImageUrl(imagePath) to get full image URL');
})();


