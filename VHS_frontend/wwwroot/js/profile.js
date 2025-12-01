// Profile Page JavaScript
document.addEventListener('DOMContentLoaded', function() {
    'use strict';

    console.log('Profile page loaded');

    // ==================== Image Upload ====================
    const imageInput = document.getElementById('imageInput');
    const profileAvatar = document.getElementById('profileAvatar');

    if (imageInput && profileAvatar) {
        imageInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (!file) return;

            // Validate file type
            if (!file.type.startsWith('image/')) {
                alert('Vui lòng chọn file ảnh hợp lệ.');
                return;
            }

            // Validate file size (5MB max)
            if (file.size > 5 * 1024 * 1024) {
                alert('Kích thước file không được quá 5MB.');
                return;
            }

            // Preview image
            const reader = new FileReader();
            reader.onload = function(event) {
                profileAvatar.src = event.target.result;
            };
            reader.readAsDataURL(file);

            // Upload to server
            const formData = new FormData();
            formData.append('image', file);

            fetch('/Customer/Profile/UploadImage', {
                method: 'POST',
                body: formData
            })
            .then(response => response.json())
            .then(data => {
                console.log('Upload response:', data);
                if (data.success) {
                    showToast('Đổi ảnh đại diện thành công!', 'success');
                    
                    // Backend trả về imageUrl hoặc data.imageUrl
                    let imageUrl = null;
                    
                    // Check nested data object
                    if (data.data && typeof data.data === 'object') {
                        imageUrl = data.data.imageUrl || data.data.ImagePath;
                    } else {
                        imageUrl = data.imageUrl || data.ImagePath;
                    }
                    
                    console.log('Image URL:', imageUrl);
                    
                    if (imageUrl) {
                        // Nếu là GCS URL (đã có http/https), dùng trực tiếp
                        // Nếu là relative path, thêm base URL (backward compatibility)
                        if (!imageUrl.startsWith('http')) {
                            // Nếu là path cũ từ wwwroot, vẫn hỗ trợ
                            if (imageUrl.startsWith('/wwwroot/')) {
                                imageUrl = 'https://apivhs.cuahangkinhdoanh.com' + imageUrl;
                            } else {
                                // Nếu là relative path, thêm base URL
                                imageUrl = 'https://apivhs.cuahangkinhdoanh.com' + (imageUrl.startsWith('/') ? imageUrl : '/' + imageUrl);
                            }
                        }
                        
                        // Update avatar immediately
                        profileAvatar.src = imageUrl + '?t=' + new Date().getTime();
                        
                        // Reload page after 1 second to get updated data
                        setTimeout(() => {
                            window.location.reload();
                        }, 1000);
                    }
                } else {
                    showToast(data.message || 'Lỗi khi đổi ảnh', 'error');
                }
            })
            .catch(error => {
                console.error('Upload error:', error);
                showToast('Lỗi khi đổi ảnh', 'error');
            });
        });
    }

    // ==================== Change Password ====================
    const changePasswordForm = document.getElementById('changePasswordForm');
    const btnRequestOTP = document.getElementById('btnRequestOTP');
    const otpTimer = document.getElementById('otpTimer');

    if (btnRequestOTP) {
        btnRequestOTP.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            console.log('Request OTP clicked');
            
            fetch('/Customer/Profile/RequestPasswordChangeOTP', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            })
            .then(response => response.json())
            .then(data => {
                console.log('OTP Response:', data);
                if (data.success) {
                    showToast('Mã OTP đã được gửi đến email của bạn', 'success');
                    startOtpTimer(900); // 15 minutes
                } else {
                    showToast(data.message || 'Không thể gửi mã OTP', 'error');
                }
            })
            .catch(error => {
                console.error('Request OTP error:', error);
                showToast('Lỗi khi gửi mã OTP', 'error');
            });
        });
    }

    if (changePasswordForm) {
        changePasswordForm.addEventListener('submit', function(e) {
            e.preventDefault();
            console.log('Change password form submitted');

            const currentPassword = changePasswordForm.querySelector('[name="CurrentPassword"]').value;
            const newPassword = changePasswordForm.querySelector('[name="NewPassword"]').value;
            const confirmPassword = changePasswordForm.querySelector('[name="ConfirmPassword"]').value;
            const otp = changePasswordForm.querySelector('[name="OTP"]').value;

            if (newPassword !== confirmPassword) {
                showToast('Mật khẩu xác nhận không khớp', 'error');
                return;
            }

            fetch('/Customer/Profile/ChangePassword', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    CurrentPassword: currentPassword,
                    NewPassword: newPassword,
                    ConfirmPassword: confirmPassword,
                    OTP: otp
                })
            })
            .then(response => response.json())
            .then(data => {
                console.log('Change password response:', data);
                if (data.success) {
                    showToast('Đổi mật khẩu thành công!', 'success');
                    // Close modal
                    const modal = bootstrap.Modal.getInstance(document.getElementById('changePasswordModal'));
                    if (modal) modal.hide();
                    changePasswordForm.reset();
                } else {
                    showToast(data.message || 'Lỗi khi đổi mật khẩu', 'error');
                }
            })
            .catch(error => {
                console.error('Change password error:', error);
                showToast('Lỗi khi đổi mật khẩu', 'error');
            });
        });
    }

    // ==================== Change Email ====================
    const changeEmailForm = document.getElementById('changeEmailForm');
    const btnRequestEmailOTP = document.getElementById('btnRequestEmailOTP');
    const emailOtpTimer = document.getElementById('emailOtpTimer');

    if (btnRequestEmailOTP) {
        btnRequestEmailOTP.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            console.log('Request Email OTP clicked');
            
            fetch('/Customer/Profile/RequestEmailChangeOTP', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            })
            .then(response => response.json())
            .then(data => {
                console.log('Email OTP Response:', data);
                if (data.success) {
                    showToast('Mã OTP đã được gửi đến email hiện tại của bạn', 'success');
                    startEmailOtpTimer(900); // 15 minutes
                } else {
                    showToast(data.message || 'Không thể gửi mã OTP', 'error');
                }
            })
            .catch(error => {
                console.error('Request email OTP error:', error);
                showToast('Lỗi khi gửi mã OTP', 'error');
            });
        });
    }

    if (changeEmailForm) {
        changeEmailForm.addEventListener('submit', function(e) {
            e.preventDefault();
            console.log('Change email form submitted');

            const newEmail = changeEmailForm.querySelector('[name="NewEmail"]').value;
            const otpCode = changeEmailForm.querySelector('[name="OtpCode"]').value;

            fetch('/Customer/Profile/ChangeEmail', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    NewEmail: newEmail,
                    OtpCode: otpCode
                })
            })
            .then(response => response.json())
            .then(data => {
                console.log('Change email response:', data);
                if (data.success) {
                    showToast('Đổi email thành công! Vui lòng đăng nhập lại.', 'success');
                    // Close modal
                    const modal = bootstrap.Modal.getInstance(document.getElementById('changeEmailModal'));
                    if (modal) modal.hide();
                    changeEmailForm.reset();
                    
                    // Redirect to login after 2 seconds
                    setTimeout(() => {
                        window.location.href = '/Account/Login';
                    }, 2000);
                } else {
                    showToast(data.message || 'Lỗi khi đổi email', 'error');
                }
            })
            .catch(error => {
                console.error('Change email error:', error);
                showToast('Lỗi khi đổi email', 'error');
            });
        });
    }

    // ==================== Helper Functions ====================

    function startOtpTimer(seconds) {
        if (!otpTimer) return;
        
        let remaining = seconds;
        const updateTimer = () => {
            const minutes = Math.floor(remaining / 60);
            const secs = remaining % 60;
            otpTimer.textContent = `Mã OTP sẽ hết hạn sau ${minutes}:${secs.toString().padStart(2, '0')}`;
            
            if (remaining > 0) {
                remaining--;
                setTimeout(updateTimer, 1000);
            } else {
                otpTimer.textContent = 'Mã OTP đã hết hạn';
            }
        };
        updateTimer();
    }

    function startEmailOtpTimer(seconds) {
        if (!emailOtpTimer) return;
        
        let remaining = seconds;
        const updateTimer = () => {
            const minutes = Math.floor(remaining / 60);
            const secs = remaining % 60;
            emailOtpTimer.textContent = `Mã OTP sẽ hết hạn sau ${minutes}:${secs.toString().padStart(2, '0')}`;
            
            if (remaining > 0) {
                remaining--;
                setTimeout(updateTimer, 1000);
            } else {
                emailOtpTimer.textContent = 'Mã OTP đã hết hạn';
            }
        };
        updateTimer();
    }

    function showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toastContainer') || createToastContainer();
        
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type === 'success' ? 'success' : 'danger'} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        toastContainer.appendChild(toast);
        
        const bsToast = new bootstrap.Toast(toast, { autohide: true, delay: 3000 });
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }

    function createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    console.log('All event listeners attached');
});
