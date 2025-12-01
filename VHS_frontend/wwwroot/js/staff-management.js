/**
 * Staff Management Service
 * Handles all API calls for staff management functionality
 */
class StaffManagementService {
    constructor() {
        this.baseURL = window.location.origin;
        this.token = localStorage.getItem('JWToken');
        this.accountId = localStorage.getItem('AccountID');
    }

    /**
     * Set authentication header for requests
     */
    setAuthHeader() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json'
        };
    }

    /**
     * Get all staff by provider ID
     * @param {string} providerId - Provider ID
     * @returns {Promise<Array>} List of staff
     */
    async getStaffByProvider(providerId) {
        try {
            // Use backend API directly instead of frontend route
            const url = `http://apivhs.cuahangkinhdoanh.com/api/staff/provider/${providerId}`;
            console.log('Fetching staff from URL:', url);
            console.log('Provider ID:', providerId);
            
            const response = await fetch(url, {
                method: 'GET',
                headers: this.setAuthHeader()
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching staff list:', error);
            throw error;
        }
    }

    /**
     * Get staff by ID
     * @param {string} staffId - Staff ID
     * @returns {Promise<Object>} Staff details
     */
    async getStaffById(staffId) {
        try {
            const response = await fetch(`http://apivhs.cuahangkinhdoanh.com/api/staff/${staffId}`, {
                method: 'GET',
                headers: this.setAuthHeader()
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching staff:', error);
            throw error;
        }
    }

    /**
     * Create new staff
     * @param {string} providerId - Provider ID
     * @param {Object} staffData - Staff data
     * @returns {Promise<Object>} Created staff
     */
    async createStaff(providerId, staffData) {
        try {
            console.log('Creating staff with data:', staffData);
            console.log('Provider ID:', providerId);
            
            // Tạo FormData cho multipart/form-data
            const formData = new FormData();
            formData.append('StaffName', staffData.staffName);
            formData.append('Password', staffData.password);
            formData.append('CitizenID', staffData.citizenID);

            // Thêm ảnh chân dung (1 ảnh duy nhất)
            if (staffData.faceImage) {
                formData.append('FaceImage', staffData.faceImage);
            }

            // Thêm ảnh CCCD
            if (staffData.citizenIDFrontImage) {
                formData.append('CitizenIDFrontImage', staffData.citizenIDFrontImage);
            }
            if (staffData.citizenIDBackImage) {
                formData.append('CitizenIDBackImage', staffData.citizenIDBackImage);
            }

            console.log('FormData contents:');
            for (let [key, value] of formData.entries()) {
                console.log(key, value);
            }

            const response = await fetch(`http://apivhs.cuahangkinhdoanh.com/api/staff/provider/${providerId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.token}`
                    // Không set Content-Type, để browser tự động set multipart/form-data
                },
                body: formData
            });

            console.log('Response status:', response.status);
            console.log('Response headers:', response.headers);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Error response:', errorText);
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }

            const result = await response.json();
            console.log('Create staff result:', result);
            return result;
        } catch (error) {
            console.error('Error creating staff:', error);
            throw error;
        }
    }

    /**
     * Update staff
     * @param {string} staffId - Staff ID
     * @param {Object} staffData - Updated staff data
     * @returns {Promise<Object>} Update result
     */
    async updateStaff(staffId, staffData) {
        try {
            console.log('Updating staff with data:', staffData);
            console.log('Staff ID:', staffId);
            
            // Tạo object update data
            const updateData = {
                staffName: staffData.staffName,
                citizenID: staffData.citizenID
            };

            // Thêm ảnh nếu có
            if (staffData.faceImage) {
                updateData.faceImage = staffData.faceImage;
            }
            if (staffData.citizenIDFrontImage) {
                updateData.citizenIDFrontImage = staffData.citizenIDFrontImage;
            }
            if (staffData.citizenIDBackImage) {
                updateData.citizenIDBackImage = staffData.citizenIDBackImage;
            }

            console.log('Update data:', updateData);

            const response = await fetch(`http://apivhs.cuahangkinhdoanh.com/api/staff/${staffId}`, {
                method: 'PUT',
                headers: this.setAuthHeader(),
                body: JSON.stringify(updateData)
            });

            console.log('Update response status:', response.status);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Update error response:', errorText);
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }

            const result = await response.json();
            console.log('Update result:', result);
            return result;
        } catch (error) {
            console.error('Error updating staff:', error);
            throw error;
        }
    }

    /**
     * Delete staff
     * @param {string} staffId - Staff ID
     * @returns {Promise<Object>} Delete result
     */
    async deleteStaff(staffId) {
        try {
            console.log('Deleting staff ID:', staffId);
            
            const response = await fetch(`http://apivhs.cuahangkinhdoanh.com/api/staff/${staffId}`, {
                method: 'DELETE',
                headers: this.setAuthHeader()
            });

            console.log('Delete response status:', response.status);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Delete error response:', errorText);
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }

            // DELETE có thể không trả về JSON
            try {
                return await response.json();
            } catch {
                return { success: true };
            }
        } catch (error) {
            console.error('Error deleting staff:', error);
            throw error;
        }
    }
}

/**
 * Staff Management Utils
 * Utility functions for staff management
 */
class StaffManagementUtils {
    /**
     * Format staff name for display
     * @param {string} name - Staff name
     * @returns {string} Formatted name
     */
    static formatStaffName(name) {
        return name.trim().toUpperCase();
    }

    /**
     * Validate staff data
     * @param {Object} staffData - Staff data to validate
     * @returns {Object} Validation result
     */
    static validateStaffData(staffData) {
        const errors = {};

        if (!staffData.staffName || staffData.staffName.trim().length === 0) {
            errors.staffName = 'Tên nhân viên không được để trống';
        }

        if (!staffData.citizenID || staffData.citizenID.trim().length < 9) {
            errors.citizenID = 'CCCD/CMND phải từ 9-12 ký tự';
        }

        if (staffData.faceImage && !this.isValidUrl(staffData.faceImage)) {
            errors.faceImage = 'URL hình ảnh khuôn mặt không hợp lệ';
        }

        if (staffData.citizenIDImage && !this.isValidUrl(staffData.citizenIDImage)) {
            errors.citizenIDImage = 'URL hình ảnh CCCD không hợp lệ';
        }

        return {
            isValid: Object.keys(errors).length === 0,
            errors: errors
        };
    }

    /**
     * Validate URL
     * @param {string} url - URL to validate
     * @returns {boolean} Is valid URL
     */
    static isValidUrl(url) {
        try {
            new URL(url);
            return true;
        } catch {
            return false;
        }
    }

    /**
     * Format citizen ID for display
     * @param {string} citizenID - Citizen ID
     * @returns {string} Formatted citizen ID
     */
    static formatCitizenID(citizenID) {
        return citizenID.replace(/(\d{3})(\d{3})(\d{3})/, '$1 $2 $3');
    }

    /**
     * Get staff avatar placeholder
     * @param {string} name - Staff name
     * @returns {string} Avatar placeholder
     */
    static getAvatarPlaceholder(name) {
        return name.charAt(0).toUpperCase();
    }
}

/**
 * Staff Management Component
 * React-like component for staff management
 */
class StaffManagementComponent {
    constructor() {
        this.service = new StaffManagementService();
        this.utils = StaffManagementUtils;
        this.staffList = [];
        this.loading = false;
        this.errors = {};
    }

    /**
     * Initialize component
     */
    async init() {
        try {
            console.log('Initializing staff management component...');
            
            // Kiểm tra xem đã có data từ server chưa
            const existingStaff = document.querySelectorAll('.staff-item');
            if (existingStaff.length > 0) {
                console.log('Staff data already loaded from server, skipping API call');
                return;
            }

            this.showLoading(true);
            await this.loadStaffList();
        } catch (error) {
            console.error('Error initializing component:', error);
            this.showError('Không thể tải danh sách nhân viên: ' + error.message);
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Load staff list
     */
    async loadStaffList() {
        try {
            console.log('Loading staff list...');
            
            // Kiểm tra xem đã có data từ server chưa
            const existingStaff = document.querySelectorAll('.staff-item');
            if (existingStaff.length > 0) {
                console.log('Staff data already loaded from server, skipping API call');
                return;
            }
            
            // Lấy providerId từ server
            let providerId = null;
            try {
                const response = await fetch('/Provider/StaffManagement/GetCurrentProviderId');
                if (response.ok) {
                    const data = await response.json();
                    providerId = data.providerId;
                    console.log('Got providerId from server:', providerId);
                } else {
                    console.error('Cannot get provider ID from server');
                    this.showError('Không thể lấy thông tin Provider');
                    return;
                }
            } catch (err) {
                console.error('Error getting provider ID:', err);
                this.showError('Không thể kết nối đến server');
                return;
            }
            
            if (!providerId) {
                this.showError('Không tìm thấy Provider ID');
                return;
            }
            
            console.log('Using providerId:', providerId);
            this.staffList = await this.service.getStaffByProvider(providerId);
            console.log('Staff list loaded:', this.staffList);
            this.renderStaffList();
        } catch (error) {
            console.error('Error loading staff list:', error);
            this.showError('Không thể tải danh sách nhân viên: ' + error.message);
        }
    }

    /**
     * Create new staff
     * @param {Object} staffData - Staff data
     */
    async createStaff(staffData) {
        try {
            this.showLoading(true);
            this.clearErrors();

            // Validation cơ bản
            if (!staffData.staffName || !staffData.citizenID || !staffData.password) {
                this.showError('Vui lòng điền đầy đủ thông tin bắt buộc');
                return;
            }

            if (!staffData.faceImage || !staffData.citizenIDFrontImage || !staffData.citizenIDBackImage) {
                this.showError('Vui lòng tải lên đầy đủ ảnh bắt buộc');
                return;
            }

            // Lấy provider ID
            let providerId = this.service.accountId;
            if (!providerId) {
                // Thử lấy từ server
                try {
                    const response = await fetch('/Provider/StaffManagement/GetCurrentProviderId');
                    if (response.ok) {
                        const data = await response.json();
                        providerId = data.providerId;
                    }
                } catch (err) {
                    console.error('Error getting provider ID:', err);
                }
            }

            if (!providerId) {
                this.showError('Không thể lấy thông tin Provider');
                return;
            }

            console.log('Creating staff with provider ID:', providerId);
            await this.service.createStaff(providerId, staffData);
            this.showSuccess('Tạo nhân viên thành công!');
            await this.loadStaffList();
            this.resetForm();
        } catch (error) {
            console.error('Error creating staff:', error);
            this.handleApiError(error);
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Update staff
     * @param {string} staffId - Staff ID
     * @param {Object} staffData - Updated staff data
     */
    async updateStaff(staffId, staffData) {
        try {
            this.showLoading(true);
            this.clearErrors();

            // Validation cơ bản
            if (!staffData.staffName || !staffData.citizenID) {
                this.showError('Vui lòng điền đầy đủ thông tin bắt buộc');
                return;
            }

            console.log('Updating staff ID:', staffId);
            await this.service.updateStaff(staffId, staffData);
            this.showSuccess('Cập nhật nhân viên thành công!');
            await this.loadStaffList();
            this.resetForm();
        } catch (error) {
            console.error('Error updating staff:', error);
            this.handleApiError(error);
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Delete staff
     * @param {string} staffId - Staff ID
     * @param {string} staffName - Staff name
     */
    async deleteStaff(staffId, staffName) {
        if (!confirm(`Bạn có chắc chắn muốn xóa nhân viên "${staffName}"?`)) {
            return;
        }

        try {
            this.showLoading(true);
            console.log('Deleting staff ID:', staffId);
            await this.service.deleteStaff(staffId);
            this.showSuccess('Xóa nhân viên thành công!');
            await this.loadStaffList();
        } catch (error) {
            console.error('Error deleting staff:', error);
            this.showError('Có lỗi xảy ra khi xóa nhân viên: ' + error.message);
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Render staff list
     */
    renderStaffList() {
        const container = document.querySelector('.staff-grid');
        if (!container) return;

        if (this.staffList.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">
                        <i class="bi bi-people"></i>
                    </div>
                    <h4>Chưa có nhân viên nào</h4>
                    <p>Hãy thêm nhân viên đầu tiên để bắt đầu quản lý</p>
                </div>
            `;
            return;
        }

        container.innerHTML = this.staffList.map(staff => `
            <div class="staff-item">
                <div class="staff-avatar">
                    ${staff.faceImage ? 
                        `<img src="${staff.faceImage}" alt="${staff.staffName}" class="avatar-img" />` :
                        `<div class="avatar-placeholder">
                            <i class="bi bi-person"></i>
                        </div>`
                    }
                </div>
                <div class="staff-info">
                    <h5 class="staff-name">${staff.staffName}</h5>
                    <p class="staff-id">
                        <i class="bi bi-card-text me-1"></i>
                        CCCD: ${staff.citizenID}
                    </p>
                    <p class="staff-username">
                        <i class="bi bi-person-badge me-1"></i>
                        Username: ${staff.username}
                    </p>
                </div>
                <div class="staff-actions">
                    <button class="btn btn-sm btn-outline-primary" onclick="editStaff('${staff.staffId}')">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteStaff('${staff.staffId}', '${staff.staffName}')">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    }

    /**
     * Show loading state
     * @param {boolean} show - Show loading
     */
    showLoading(show) {
        console.log('Loading state:', show);
        this.loading = show;
        
        // Disable/enable submit buttons
        const buttons = document.querySelectorAll('button[type="submit"]');
        buttons.forEach(btn => {
            btn.disabled = show;
            if (show) {
                btn.innerHTML = '<i class="bi bi-hourglass-split me-2"></i>Đang xử lý...';
            } else {
                // Reset button text
                if (btn.closest('form').action.includes('Create')) {
                    btn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Tạo nhân viên';
                } else if (btn.closest('form').action.includes('Edit')) {
                    btn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Cập nhật nhân viên';
                }
            }
        });
        
        // Show/hide loading overlay
        if (show) {
            this.showAlert('Đang xử lý...', 'info');
        }
    }

    /**
     * Show success message
     * @param {string} message - Success message
     */
    showSuccess(message) {
        console.log('Success:', message);
        this.showAlert(message, 'success');
    }

    /**
     * Show error message
     * @param {string} message - Error message
     */
    showError(message) {
        console.error('Error:', message);
        this.showAlert(message, 'danger');
    }

    /**
     * Show alert
     * @param {string} message - Alert message
     * @param {string} type - Alert type
     */
    showAlert(message, type) {
        console.log(`Alert [${type}]:`, message);
        
        // Tìm container phù hợp
        let alertContainer = document.querySelector('.staff-content');
        if (!alertContainer) {
            alertContainer = document.querySelector('.staff-management');
        }
        if (!alertContainer) {
            alertContainer = document.body;
        }

        const alert = document.createElement('div');
        alert.className = `alert alert-${type} alert-dismissible fade show`;
        alert.style.position = 'fixed';
        alert.style.top = '20px';
        alert.style.right = '20px';
        alert.style.zIndex = '9999';
        alert.style.minWidth = '300px';
        alert.style.maxWidth = '500px';
        alert.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="flex-grow-1">${message}</div>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        alertContainer.appendChild(alert);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove();
            }
        }, 5000);
    }

    /**
     * Show validation errors
     * @param {Object} errors - Validation errors
     */
    showErrors(errors) {
        console.log('Validation errors:', errors);
        this.errors = errors;
        
        // Hiển thị lỗi tổng quát
        if (typeof errors === 'object' && errors !== null) {
            const errorMessages = Object.values(errors);
            if (errorMessages.length > 0) {
                this.showError('Lỗi validation: ' + errorMessages.join(', '));
            }
        }
        
        // Hiển thị lỗi cho từng field
        Object.keys(errors).forEach(field => {
            const input = document.querySelector(`[name="${field}"]`);
            if (input) {
                input.classList.add('is-invalid');
                const errorDiv = input.parentNode.querySelector('.invalid-feedback') || 
                               document.createElement('div');
                errorDiv.className = 'invalid-feedback';
                errorDiv.textContent = errors[field];
                if (!input.parentNode.querySelector('.invalid-feedback')) {
                    input.parentNode.appendChild(errorDiv);
                }
            }
        });
    }

    /**
     * Clear errors
     */
    clearErrors() {
        console.log('Clearing errors');
        this.errors = {};
        
        // Xóa validation errors
        document.querySelectorAll('.is-invalid').forEach(el => {
            el.classList.remove('is-invalid');
        });
        document.querySelectorAll('.invalid-feedback').forEach(el => {
            el.remove();
        });
        
        // Xóa tất cả alerts
        document.querySelectorAll('.alert').forEach(alert => {
            if (alert.classList.contains('alert-danger') || 
                alert.classList.contains('alert-success') || 
                alert.classList.contains('alert-info')) {
                alert.remove();
            }
        });
    }

    /**
     * Handle API error
     * @param {Error} error - API error
     */
    handleApiError(error) {
        console.error('API Error:', error);
        try {
            // Thử parse JSON error
            const errorData = JSON.parse(error.message);
            this.showErrors(errorData);
        } catch {
            // Nếu không parse được JSON, hiển thị message trực tiếp
            const errorMessage = error.message || 'Có lỗi xảy ra khi xử lý yêu cầu';
            this.showError(errorMessage);
        }
    }

    /**
     * Reset form
     */
    resetForm() {
        console.log('Resetting form');
        const form = document.querySelector('form');
        if (form) {
            form.reset();
        }
        this.clearErrors();
        
        // Reset file inputs
        document.querySelectorAll('input[type="file"]').forEach(input => {
            input.value = '';
        });
        
        // Reset image previews
        document.querySelectorAll('.image-container').forEach(container => {
            const placeholder = container.parentNode.querySelector('.placeholder-container');
            if (placeholder) {
                container.outerHTML = placeholder.outerHTML;
            }
        });
        
        // Reset text inputs
        document.querySelectorAll('input[type="text"], input[type="password"]').forEach(input => {
            input.value = '';
        });
    }
}

// Global functions for HTML onclick handlers
function deleteStaff(staffId, staffName) {
    console.log('Global deleteStaff called:', staffId, staffName);
    if (window.staffComponent) {
        window.staffComponent.deleteStaff(staffId, staffName);
    } else {
        console.error('Staff component not found');
        alert('Lỗi: Component không được khởi tạo. Vui lòng refresh trang.');
    }
}

function editStaff(staffId) {
    console.log('Global editStaff called:', staffId);
    window.location.href = `/Provider/StaffManagement/Edit/${staffId}`;
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing staff management...');
    
    // Khởi tạo component cho tất cả các trang staff management
    if (document.querySelector('.staff-management')) {
        console.log('Staff management page detected, initializing component...');
        try {
            window.staffComponent = new StaffManagementComponent();
            window.staffComponent.init();
        } catch (error) {
            console.error('Error initializing staff component:', error);
            alert('Lỗi khởi tạo component: ' + error.message);
        }
    } else {
        console.log('Not a staff management page, skipping initialization');
    }
});
