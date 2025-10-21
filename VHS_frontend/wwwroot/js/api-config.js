/**
 * API Configuration for Provider Profile
 * Cấu hình API cho Provider Profile
 */

const API_CONFIG = {
    // Base URLs for different environments
    baseURLs: {
        development: 'https://localhost:7154',
        staging: 'https://staging-api.vhs.com',
        production: 'https://api.vhs.com'
    },
    
    // Current environment
    environment: 'development', // Change this to 'staging' or 'production'
    
    // API endpoints
    endpoints: {
        getProfile: '/api/provider/profile',
        updateProfile: '/api/provider/profile',
        getProviderId: '/api/provider/get-id-by-account'
    },
    
    // Request timeouts (in milliseconds)
    timeouts: {
        default: 10000,    // 10 seconds
        upload: 30000,     // 30 seconds for file uploads
        long: 60000        // 60 seconds for long operations
    },
    
    // Retry configuration
    retry: {
        maxAttempts: 3,
        delay: 1000,        // 1 second between retries
        backoff: 2          // Exponential backoff multiplier
    },
    
    // Authentication
    auth: {
        tokenKey: 'vhs_token',
        accountIdKey: 'vhs_account_id',
        refreshTokenKey: 'vhs_refresh_token'
    },
    
    // Validation rules
    validation: {
        providerName: {
            required: true,
            maxLength: 100,
            minLength: 2
        },
        phoneNumber: {
            required: true,
            pattern: /^[0-9+\-\s()]+$/,
            maxLength: 15
        },
        description: {
            required: false,
            maxLength: 500
        },
        images: {
            required: false,
            pattern: /^https?:\/\/.+/,
            maxLength: 500
        }
    },
    
    // Error messages
    messages: {
        errors: {
            network: 'Lỗi kết nối mạng. Vui lòng kiểm tra internet và thử lại.',
            timeout: 'Yêu cầu quá thời gian. Vui lòng thử lại.',
            unauthorized: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
            forbidden: 'Bạn không có quyền thực hiện thao tác này.',
            notFound: 'Không tìm thấy thông tin.',
            serverError: 'Lỗi server. Vui lòng thử lại sau.',
            validation: 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.'
        },
        success: {
            profileUpdated: 'Cập nhật profile thành công!',
            profileLoaded: 'Tải thông tin profile thành công!'
        }
    },
    
    // UI configuration
    ui: {
        loadingText: 'Đang tải...',
        savingText: 'Đang lưu...',
        successDuration: 3000,    // 3 seconds
        errorDuration: 5000      // 5 seconds
    }
};

/**
 * Get current base URL based on environment
 */
function getBaseURL() {
    return API_CONFIG.baseURLs[API_CONFIG.environment];
}

/**
 * Get full API URL for an endpoint
 */
function getApiUrl(endpoint, params = {}) {
    let url = getBaseURL() + endpoint;
    
    // Replace parameters in URL
    Object.keys(params).forEach(key => {
        url = url.replace(`{${key}}`, params[key]);
    });
    
    return url;
}

/**
 * Get authentication token
 */
function getAuthToken() {
    return localStorage.getItem(API_CONFIG.auth.tokenKey);
}

/**
 * Get account ID
 */
function getAccountId() {
    return localStorage.getItem(API_CONFIG.auth.accountIdKey);
}

/**
 * Set authentication data
 */
function setAuthData(token, accountId, refreshToken = null) {
    localStorage.setItem(API_CONFIG.auth.tokenKey, token);
    localStorage.setItem(API_CONFIG.auth.accountIdKey, accountId);
    
    if (refreshToken) {
        localStorage.setItem(API_CONFIG.auth.refreshTokenKey, refreshToken);
    }
}

/**
 * Clear authentication data
 */
function clearAuthData() {
    localStorage.removeItem(API_CONFIG.auth.tokenKey);
    localStorage.removeItem(API_CONFIG.auth.accountIdKey);
    localStorage.removeItem(API_CONFIG.auth.refreshTokenKey);
}

/**
 * Check if user is authenticated
 */
function isAuthenticated() {
    return !!(getAuthToken() && getAccountId());
}

/**
 * Get request headers
 */
function getRequestHeaders() {
    const headers = {
        'Content-Type': 'application/json'
    };
    
    const token = getAuthToken();
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    return headers;
}

/**
 * Validate form data according to configuration
 */
function validateFormData(data) {
    const errors = {};
    const rules = API_CONFIG.validation;
    
    // Validate provider name
    if (rules.providerName.required && (!data.providerName || data.providerName.trim() === '')) {
        errors.providerName = ['Tên nhà cung cấp không được để trống'];
    } else if (data.providerName && data.providerName.length > rules.providerName.maxLength) {
        errors.providerName = [`Tên nhà cung cấp không được vượt quá ${rules.providerName.maxLength} ký tự`];
    } else if (data.providerName && data.providerName.length < rules.providerName.minLength) {
        errors.providerName = [`Tên nhà cung cấp phải có ít nhất ${rules.providerName.minLength} ký tự`];
    }
    
    // Validate phone number
    if (rules.phoneNumber.required && (!data.phoneNumber || data.phoneNumber.trim() === '')) {
        errors.phoneNumber = ['Số điện thoại không được để trống'];
    } else if (data.phoneNumber && !rules.phoneNumber.pattern.test(data.phoneNumber)) {
        errors.phoneNumber = ['Số điện thoại không đúng định dạng'];
    } else if (data.phoneNumber && data.phoneNumber.length > rules.phoneNumber.maxLength) {
        errors.phoneNumber = [`Số điện thoại không được vượt quá ${rules.phoneNumber.maxLength} ký tự`];
    }
    
    // Validate description
    if (data.description && data.description.length > rules.description.maxLength) {
        errors.description = [`Mô tả không được vượt quá ${rules.description.maxLength} ký tự`];
    }
    
    // Validate images URL
    if (data.images && data.images.trim() !== '') {
        if (!rules.images.pattern.test(data.images)) {
            errors.images = ['URL hình ảnh không đúng định dạng'];
        } else if (data.images.length > rules.images.maxLength) {
            errors.images = [`URL hình ảnh không được vượt quá ${rules.images.maxLength} ký tự`];
        }
    }
    
    return errors;
}

/**
 * Get error message by status code
 */
function getErrorMessage(status) {
    const messages = API_CONFIG.messages.errors;
    
    switch (status) {
        case 0:
        case -1:
            return messages.network;
        case 401:
            return messages.unauthorized;
        case 403:
            return messages.forbidden;
        case 404:
            return messages.notFound;
        case 408:
            return messages.timeout;
        case 422:
            return messages.validation;
        case 500:
        case 502:
        case 503:
        case 504:
            return messages.serverError;
        default:
            return messages.serverError;
    }
}

/**
 * Create API request with retry logic
 */
async function createApiRequest(url, options = {}, retryCount = 0) {
    const maxRetries = API_CONFIG.retry.maxAttempts;
    const delay = API_CONFIG.retry.delay * Math.pow(API_CONFIG.retry.backoff, retryCount);
    
    try {
        const response = await fetch(url, {
            ...options,
            headers: {
                ...getRequestHeaders(),
                ...options.headers
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        return response;
    } catch (error) {
        if (retryCount < maxRetries && (error.name === 'TypeError' || error.message.includes('fetch'))) {
            console.log(`Retry ${retryCount + 1}/${maxRetries} after ${delay}ms`);
            await new Promise(resolve => setTimeout(resolve, delay));
            return createApiRequest(url, options, retryCount + 1);
        }
        
        throw error;
    }
}

// Export configuration
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        API_CONFIG,
        getBaseURL,
        getApiUrl,
        getAuthToken,
        getAccountId,
        setAuthData,
        clearAuthData,
        isAuthenticated,
        getRequestHeaders,
        validateFormData,
        getErrorMessage,
        createApiRequest
    };
}

// Make functions available globally
if (typeof window !== 'undefined') {
    window.API_CONFIG = API_CONFIG;
    window.getBaseURL = getBaseURL;
    window.getApiUrl = getApiUrl;
    window.getAuthToken = getAuthToken;
    window.getAccountId = getAccountId;
    window.setAuthData = setAuthData;
    window.clearAuthData = clearAuthData;
    window.isAuthenticated = isAuthenticated;
    window.getRequestHeaders = getRequestHeaders;
    window.validateFormData = validateFormData;
    window.getErrorMessage = getErrorMessage;
    window.createApiRequest = createApiRequest;
}
