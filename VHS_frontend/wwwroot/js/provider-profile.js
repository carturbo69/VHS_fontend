/**
 * Provider Profile Service - JavaScript API Client
 * Cung cấp các method để gọi API Profile của Provider
 * Sử dụng pattern tương tự như các service khác trong dự án
 */
class ProviderProfileService {
    constructor() {
        this.baseURL = window.location.origin; // Sử dụng cùng domain với FE
        this.token = this.getAuthToken();
    }

    /**
     * Lấy token từ localStorage
     */
    getAuthToken() {
        return localStorage.getItem('vhs_token') || sessionStorage.getItem('vhs_token');
    }

    /**
     * Lấy account ID từ localStorage
     */
    getAccountId() {
        return localStorage.getItem('vhs_account_id') || sessionStorage.getItem('vhs_account_id');
    }

    /**
     * Tạo headers cho request
     */
    getRequestHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };
        
        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        return headers;
    }

    /**
     * Lấy thông tin profile của Provider
     * @param {string} accountId - ID của tài khoản (optional, sẽ lấy từ localStorage nếu không có)
     * @returns {Promise<Object>} Thông tin profile
     */
    async getProfile(accountId = null) {
        try {
            const id = accountId || this.getAccountId();
            if (!id) {
                throw new Error('Account ID not found');
            }

            const response = await fetch(`${this.baseURL}/Provider/ProviderProfile/api/provider/profile/${id}`, {
                method: 'GET',
                headers: this.getRequestHeaders()
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching profile:', error);
            throw error;
        }
    }

    /**
     * Cập nhật thông tin profile của Provider
     * @param {Object} profileData - Dữ liệu profile cần cập nhật
     * @param {string} accountId - ID của tài khoản (optional, sẽ lấy từ localStorage nếu không có)
     * @returns {Promise<Object>} Kết quả cập nhật
     */
    async updateProfile(profileData, accountId = null) {
        try {
            const id = accountId || this.getAccountId();
            if (!id) {
                throw new Error('Account ID not found');
            }

            const response = await fetch(`${this.baseURL}/Provider/ProviderProfile/api/provider/profile/${id}`, {
                method: 'PUT',
                headers: this.getRequestHeaders(),
                body: JSON.stringify(profileData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(JSON.stringify(errorData));
            }

            return await response.json();
        } catch (error) {
            console.error('Error updating profile:', error);
            throw error;
        }
    }

    /**
     * Lấy Provider ID từ Account ID
     * @param {string} accountId - ID của tài khoản (optional, sẽ lấy từ localStorage nếu không có)
     * @returns {Promise<string>} Provider ID
     */
    async getProviderId(accountId = null) {
        try {
            const id = accountId || this.getAccountId();
            if (!id) {
                throw new Error('Account ID not found');
            }

            const response = await fetch(`${this.baseURL}/Provider/ProviderProfile/api/provider/get-id-by-account/${id}`, {
                method: 'GET',
                headers: this.getRequestHeaders()
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error getting provider ID:', error);
            throw error;
        }
    }
}

/**
 * Utility functions for Provider Profile
 */
const ProviderProfileUtils = {
    /**
     * Khởi tạo service (không cần tham số vì service tự lấy token)
     * @returns {ProviderProfileService} Service instance
     */
    initService() {
        return new ProviderProfileService();
    },

    /**
     * Hiển thị thông báo lỗi
     * @param {string} message - Thông báo lỗi
     * @param {string} type - Loại thông báo (error, success, warning)
     */
    showNotification(message, type = 'error') {
        // Tạo thông báo Bootstrap
        const alertClass = type === 'error' ? 'alert-danger' : 
                          type === 'success' ? 'alert-success' : 'alert-warning';
        
        const alertHtml = `
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        
        // Thêm vào đầu container
        const container = document.querySelector('.container-fluid, .container');
        if (container) {
            container.insertAdjacentHTML('afterbegin', alertHtml);
        }
    },

    /**
     * Xử lý lỗi validation từ API
     * @param {Object} errorData - Dữ liệu lỗi từ API
     * @returns {Object} Object chứa các lỗi validation
     */
    handleValidationErrors(errorData) {
        const errors = {};
        try {
            const parsed = JSON.parse(errorData.message || errorData);
            Object.keys(parsed).forEach(field => {
                errors[field] = parsed[field];
            });
        } catch (e) {
            console.error('Error parsing validation errors:', e);
        }
        return errors;
    },

    /**
     * Hiển thị lỗi validation trên form
     * @param {Object} errors - Object chứa các lỗi validation
     */
    displayValidationErrors(errors) {
        // Xóa các lỗi cũ
        document.querySelectorAll('.validation-error').forEach(el => el.remove());
        
        // Hiển thị lỗi mới
        Object.keys(errors).forEach(field => {
            const input = document.querySelector(`[name="${field}"]`);
            if (input) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'validation-error text-danger mt-1';
                errorDiv.textContent = errors[field][0] || errors[field];
                
                const formGroup = input.closest('.mb-3, .form-group');
                if (formGroup) {
                    formGroup.appendChild(errorDiv);
                }
            }
        });
    }
};

/**
 * React Component cho Provider Profile (nếu sử dụng React)
 */
const ProviderProfileComponent = {
    /**
     * Khởi tạo component
     */
    init() {
        this.service = ProviderProfileUtils.initService();
        this.loadProfile();
    },

    /**
     * Tải thông tin profile
     */
    async loadProfile() {
        try {
            const profile = await this.service.getProfile();
            this.displayProfile(profile);
        } catch (error) {
            console.error('Error loading profile:', error);
            ProviderProfileUtils.showNotification('Không thể tải thông tin profile', 'error');
        }
    },

    /**
     * Hiển thị thông tin profile
     * @param {Object} profile - Thông tin profile
     */
    displayProfile(profile) {
        // Cập nhật các field trong form
        const fields = ['providerName', 'phoneNumber', 'description', 'images'];
        fields.forEach(field => {
            const input = document.querySelector(`[name="${field}"]`);
            if (input && profile[field]) {
                input.value = profile[field];
            }
        });

        // Hiển thị thông tin readonly
        const readonlyFields = ['email', 'status'];
        readonlyFields.forEach(field => {
            const element = document.querySelector(`[data-field="${field}"]`);
            if (element && profile[field]) {
                element.textContent = profile[field];
            }
        });
    },

    /**
     * Cập nhật profile
     * @param {Object} formData - Dữ liệu form
     */
    async updateProfile(formData) {
        try {
            const result = await this.service.updateProfile(formData);
            ProviderProfileUtils.showNotification('Cập nhật profile thành công!', 'success');
            
            // Reload profile
            await this.loadProfile();
        } catch (error) {
            console.error('Error updating profile:', error);
            
            try {
                const errors = ProviderProfileUtils.handleValidationErrors(error);
                ProviderProfileUtils.displayValidationErrors(errors);
            } catch (e) {
                ProviderProfileUtils.showNotification('Có lỗi xảy ra khi cập nhật profile', 'error');
            }
        }
    }
};

// Export cho module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ProviderProfileService, ProviderProfileUtils, ProviderProfileComponent };
}
