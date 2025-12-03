/**
 * Provider Schedule Management
 * Handles schedule creation, editing, deletion with smart notifications
 */

// ============================================
// GLOBAL VARIABLES & FUNCTIONS (Outside document.ready)
// ============================================

let confirmCallback = null;

// Translate error messages from English to Vietnamese
function translateError(errorText) {
    if (!errorText) return null;
    
    // Try to parse JSON if it's a JSON string
    let message = errorText;
    try {
        const parsed = JSON.parse(errorText);
        if (parsed.message) {
            message = parsed.message;
        } else if (parsed.data && typeof parsed.data === 'string') {
            message = parsed.data;
        } else if (typeof parsed === 'string') {
            message = parsed;
        }
    } catch (e) {
        // Not JSON, use as is
        message = errorText;
    }
    
    const lowerText = message.toLowerCase();
    
    // Error message translations (sắp xếp từ dài đến ngắn để match chính xác hơn)
    const translations = {
        'time-off conflicts with existing bookings': 'Ngày nghỉ trùng với đơn đặt lịch hiện có',
        'time-off already exists for this date': 'Ngày nghỉ đã tồn tại cho ngày này',
        'schedule already exists for this day': 'Lịch làm việc đã tồn tại cho ngày này',
        'time-off already exists': 'Ngày nghỉ đã tồn tại',
        'schedule already exists': 'Lịch làm việc đã tồn tại',
        'already exists for this date': 'Đã tồn tại cho ngày này',
        'conflicts with existing bookings': 'Trùng với đơn đặt lịch hiện có',
        'conflicts with existing': 'Trùng với dữ liệu hiện có',
        'cannot create time-off': 'Không thể tạo ngày nghỉ',
        'cannot add day': 'Không thể thêm ngày',
        'cannot create schedule': 'Không thể tạo lịch làm việc',
        'cannot update schedule': 'Không thể cập nhật lịch làm việc',
        'cannot delete schedule': 'Không thể xóa lịch làm việc',
        'cannot delete time-off': 'Không thể xóa ngày nghỉ',
        'already exists': 'Đã tồn tại',
        'unauthorized': 'Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn',
        'not found': 'Không tìm thấy dữ liệu',
        'bad request': 'Yêu cầu không hợp lệ',
        'internal server error': 'Lỗi hệ thống. Vui lòng thử lại sau',
        'conflicts': 'Xung đột dữ liệu'
    };
    
    // Check for exact matches (từ dài đến ngắn)
    const sortedKeys = Object.keys(translations).sort((a, b) => b.length - a.length);
    for (const key of sortedKeys) {
        if (lowerText.includes(key)) {
            return translations[key];
        }
    }
    
    // Return original message if no translation found
    return message;
}

// Toast Notification Function (Global)
window.showToast = function(type, title, message) {
    const toastContainer = document.getElementById('toastContainer');
    const toastId = 'toast-' + Date.now();
    
    const iconMap = {
        'success': 'bi-check-circle-fill',
        'error': 'bi-x-circle-fill',
        'info': 'bi-info-circle-fill'
    };
    
    const titleMap = {
        'success': title || 'Thành công',
        'error': title || 'Lỗi',
        'info': title || 'Thông báo'
    };
    
    // Translate error messages
    let displayMessage = message;
    if (type === 'error' && message) {
        const translated = translateError(message);
        if (translated) {
            displayMessage = translated;
        }
    }
    
    const toast = document.createElement('div');
    toast.className = 'toast-custom';
    toast.id = toastId;
    toast.innerHTML = `
        <div class="toast-header-custom ${type}">
            <i class="bi ${iconMap[type]}"></i>
            <strong>${titleMap[type]}</strong>
            <button class="toast-close" onclick="closeToast('${toastId}')">&times;</button>
        </div>
        <div class="toast-body-custom">
            ${displayMessage}
        </div>
    `;
    
    toastContainer.appendChild(toast);
    
    setTimeout(() => {
        closeToast(toastId);
    }, 5000);
};

window.closeToast = function(toastId) {
    const toast = document.getElementById(toastId);
    if (toast) {
        toast.classList.add('hiding');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }
};

// Confirmation Modal Function (Global)
window.showConfirmModal = function(options) {
    document.getElementById('confirmModalTitle').textContent = options.title || 'Xác nhận';
    document.getElementById('confirmModalMessage').textContent = options.message;
    document.getElementById('confirmModalDetails').innerHTML = options.details || '';
    
    confirmCallback = options.onConfirm;
    
    const modal = new bootstrap.Modal(document.getElementById('confirmModal'));
    modal.show();
};

// Helper function to show loading state
function setLoading(isLoading) {
    const buttons = document.querySelectorAll('button[type="submit"], .btn-danger, .btn-primary');
    buttons.forEach(btn => {
        if (isLoading) {
            btn.disabled = true;
            btn.style.opacity = '0.6';
        } else {
            btn.disabled = false;
            btn.style.opacity = '1';
        }
    });
}

// Validation Helper Functions
function clearError(fieldId) {
    const errorElement = document.getElementById(fieldId + '-error');
    const inputElement = document.getElementById(fieldId);
    if (errorElement) {
        errorElement.classList.remove('show');
        errorElement.textContent = '';
    }
    if (inputElement) {
        inputElement.classList.remove('is-invalid');
    }
}

function showError(fieldId, message) {
    const errorElement = document.getElementById(fieldId + '-error');
    const inputElement = document.getElementById(fieldId);
    if (errorElement) {
        // Clear existing content first to prevent duplicate messages
        errorElement.textContent = '';
        errorElement.textContent = message;
        errorElement.classList.add('show');
    }
    if (inputElement) {
        inputElement.classList.add('is-invalid');
    }
}

function clearAllErrors(formId) {
    const form = document.getElementById(formId);
    if (!form) return;
    
    const errorElements = form.querySelectorAll('.error-message');
    const inputElements = form.querySelectorAll('.schedule-form-control');
    
    errorElements.forEach(el => {
        el.classList.remove('show');
        el.textContent = '';
    });
    
    inputElements.forEach(el => {
        el.classList.remove('is-invalid');
    });
}

function validateWeeklyForm() {
    let isValid = true;
    
    // Validate days selection
    const daysSelected = [];
    for (let i = 0; i <= 6; i++) {
        if (document.getElementById('day' + i).checked) {
            daysSelected.push(i);
        }
    }
    
    if (daysSelected.length === 0) {
        showError('days', 'Vui lòng chọn ít nhất một ngày trong tuần');
        isValid = false;
    } else {
        clearError('days');
    }
    
    // Validate start time
    const startTime = document.getElementById('weeklyStartTime').value;
    if (!startTime) {
        showError('weeklyStartTime', 'Vui lòng chọn giờ bắt đầu');
        isValid = false;
    } else {
        clearError('weeklyStartTime');
    }
    
    // Validate end time
    const endTime = document.getElementById('weeklyEndTime').value;
    if (!endTime) {
        showError('weeklyEndTime', 'Vui lòng chọn giờ kết thúc');
        isValid = false;
    } else {
        clearError('weeklyEndTime');
    }
    
    // Validate time range
    if (startTime && endTime && startTime >= endTime) {
        showError('weeklyEndTime', 'Giờ kết thúc phải lớn hơn giờ bắt đầu');
        isValid = false;
    }
    
    // Validate booking limit if provided
    const bookingLimit = document.getElementById('weeklyBookingLimit').value;
    if (bookingLimit) {
        const limit = parseInt(bookingLimit);
        if (isNaN(limit) || limit < 1 || limit > 100) {
            showError('weeklyBookingLimit', 'Giới hạn đơn phải từ 1 đến 100');
            isValid = false;
        } else {
            clearError('weeklyBookingLimit');
        }
    }
    
    return isValid;
}

function validateDailyForm() {
    let isValid = true;
    
    // Validate day of week
    const dayOfWeek = document.getElementById('dailyDayOfWeek').value;
    if (!dayOfWeek && dayOfWeek !== '0') {
        showError('dailyDayOfWeek', 'Vui lòng chọn thứ trong tuần');
        isValid = false;
    } else {
        clearError('dailyDayOfWeek');
    }
    
    // Validate start time
    const startTime = document.getElementById('dailyStartTime').value;
    if (!startTime) {
        showError('dailyStartTime', 'Vui lòng chọn giờ bắt đầu');
        isValid = false;
    } else {
        clearError('dailyStartTime');
    }
    
    // Validate end time
    const endTime = document.getElementById('dailyEndTime').value;
    if (!endTime) {
        showError('dailyEndTime', 'Vui lòng chọn giờ kết thúc');
        isValid = false;
    } else {
        clearError('dailyEndTime');
    }
    
    // Validate time range
    if (startTime && endTime && startTime >= endTime) {
        showError('dailyEndTime', 'Giờ kết thúc phải lớn hơn giờ bắt đầu');
        isValid = false;
    }
    
    // Validate booking limit if provided
    const bookingLimit = document.getElementById('dailyBookingLimit').value;
    if (bookingLimit) {
        const limit = parseInt(bookingLimit);
        if (isNaN(limit) || limit < 1 || limit > 100) {
            showError('dailyBookingLimit', 'Giới hạn đơn phải từ 1 đến 100');
            isValid = false;
        } else {
            clearError('dailyBookingLimit');
        }
    }
    
    return isValid;
}

function validateTimeOffForm() {
    let isValid = true;
    
    // Validate date
    const date = document.getElementById('timeOffDate').value;
    if (!date) {
        showError('timeOffDate', 'Vui lòng chọn ngày nghỉ');
        isValid = false;
    } else {
        clearError('timeOffDate');
    }
    
    return isValid;
}

function validateEditScheduleForm() {
    let isValid = true;
    
    // Validate start time
    const startTime = document.getElementById('editStartTime').value;
    if (!startTime) {
        showError('editStartTime', 'Vui lòng chọn giờ bắt đầu');
        isValid = false;
    } else {
        clearError('editStartTime');
    }
    
    // Validate end time
    const endTime = document.getElementById('editEndTime').value;
    if (!endTime) {
        showError('editEndTime', 'Vui lòng chọn giờ kết thúc');
        isValid = false;
    } else {
        clearError('editEndTime');
    }
    
    // Validate time range
    if (startTime && endTime && startTime >= endTime) {
        showError('editEndTime', 'Giờ kết thúc phải lớn hơn giờ bắt đầu');
        isValid = false;
    }
    
    // Validate booking limit if provided
    const bookingLimit = document.getElementById('editBookingLimit').value;
    if (bookingLimit) {
        const limit = parseInt(bookingLimit);
        if (isNaN(limit) || limit < 1 || limit > 100) {
            showError('editBookingLimit', 'Giới hạn đơn phải từ 1 đến 100');
            isValid = false;
        } else {
            clearError('editBookingLimit');
        }
    }
    
    return isValid;
}

// Delete Schedule with Smart Confirmation (Global)
window.deleteSchedule = function(id, dayName, startTime, endTime) {
    showConfirmModal({
        title: 'Xóa lịch làm việc',
        message: 'Bạn có chắc chắn muốn xóa lịch làm việc này?',
        details: `
            <div class="mb-2">
                <strong><i class="bi bi-calendar-day"></i> Ngày:</strong> ${dayName || 'N/A'}
            </div>
            <div class="mb-2">
                <strong><i class="bi bi-clock"></i> Thời gian:</strong> ${startTime || 'N/A'} - ${endTime || 'N/A'}
            </div>
        `,
        onConfirm: function() {
            setLoading(true);
            showToast('info', 'Đang xử lý', 'Đang xóa lịch làm việc...');
            
            fetch(`/Provider/ProviderSchedule/DeleteSchedule/${id}`, {
                method: 'DELETE'
            })
            .then(async res => {
                const text = await res.text();
                try {
                    return JSON.parse(text);
                } catch (e) {
                    return { success: false };
                }
            })
            .then(result => {
                setLoading(false);
                if (result.success) {
                    showToast('success', 'Đã xóa', 'Lịch làm việc đã được xóa thành công!');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    const errorMsg = translateError(result.message) || 'Không thể xóa lịch làm việc';
                    showToast('error', 'Lỗi', errorMsg);
                }
            })
            .catch(error => {
                setLoading(false);
                showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server');
            });
        }
    });
};

// Delete Time Off with Smart Confirmation (Global)
window.deleteTimeOff = function(id, date, reason) {
    showConfirmModal({
        title: 'Xóa ngày nghỉ',
        message: 'Bạn có chắc chắn muốn xóa ngày nghỉ này?',
        details: `
            <div class="mb-2">
                <strong><i class="bi bi-calendar-x"></i> Ngày:</strong> ${date || 'N/A'}
            </div>
            ${reason ? `<div class="mb-2"><strong><i class="bi bi-file-text"></i> Lý do:</strong> ${reason}</div>` : ''}
        `,
        onConfirm: function() {
            setLoading(true);
            showToast('info', 'Đang xử lý', 'Đang xóa ngày nghỉ...');
            
            fetch(`/Provider/ProviderSchedule/DeleteTimeOff/${id}`, {
                method: 'DELETE'
            })
            .then(async res => {
                const text = await res.text();
                try {
                    return JSON.parse(text);
                } catch (e) {
                    return { success: false };
                }
            })
            .then(result => {
                setLoading(false);
                if (result.success) {
                    showToast('success', 'Đã xóa', 'Ngày nghỉ đã được xóa thành công!');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    const errorMsg = translateError(result.message) || 'Không thể xóa ngày nghỉ';
                    showToast('error', 'Lỗi', errorMsg);
                }
            })
            .catch(error => {
                setLoading(false);
                showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server');
            });
        }
    });
};

// Open Edit Schedule Modal (Global)
window.openEditScheduleModal = function(scheduleId, dayName, startTime, endTime, bookingLimit) {
    document.getElementById('editScheduleId').value = scheduleId;
    document.getElementById('editScheduleDayName').textContent = dayName;
    document.getElementById('editStartTime').value = startTime;
    document.getElementById('editEndTime').value = endTime;
    document.getElementById('editBookingLimit').value = bookingLimit || '';
    
    const modal = new bootstrap.Modal(document.getElementById('editScheduleModal'));
    modal.show();
};

// Calendar Navigation Functions (Global)
window.previousMonth = function() {
    if (window.calendarCurrentMonth) {
        window.calendarCurrentMonth.setMonth(window.calendarCurrentMonth.getMonth() - 1);
        updateCalendarDisplay();
    }
};

window.nextMonth = function() {
    if (window.calendarCurrentMonth) {
        window.calendarCurrentMonth.setMonth(window.calendarCurrentMonth.getMonth() + 1);
        updateCalendarDisplay();
    }
};

window.selectAllDays = function() {
    for (let i = 0; i <= 6; i++) {
        const checkbox = document.getElementById('day' + i);
        if (checkbox) checkbox.checked = true;
    }
};

window.clearAllDays = function() {
    for (let i = 0; i <= 6; i++) {
        const checkbox = document.getElementById('day' + i);
        if (checkbox) checkbox.checked = false;
    }
};

// ============================================
// DOCUMENT READY - Initialization
// ============================================

$(document).ready(function() {
    // Calendar variables
    window.calendarCurrentMonth = new Date();
    let schedules = window.schedulesData || [];
    let timeOffs = window.timeOffsData || [];
    
    // Handle confirm button click
    const confirmButton = document.getElementById('confirmModalButton');
    if (confirmButton) {
        confirmButton.addEventListener('click', function() {
            if (confirmCallback) {
                confirmCallback();
            }
            const modalElement = document.getElementById('confirmModal');
            if (modalElement) {
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                if (modalInstance) modalInstance.hide();
            }
        });
    }
    
    // Calendar functions
    function updateCalendarDisplay() {
        const monthNames = ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
            "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"];
        const displayElement = document.getElementById('currentMonthDisplay');
        if (displayElement && window.calendarCurrentMonth) {
            displayElement.textContent = 
                `${monthNames[window.calendarCurrentMonth.getMonth()]} ${window.calendarCurrentMonth.getFullYear()}`;
        }
        renderCalendar();
    }
    
    function renderCalendar() {
        const calendar = document.getElementById('calendarMonth');
        if (!calendar || !window.calendarCurrentMonth) return;
        
        const year = window.calendarCurrentMonth.getFullYear();
        const month = window.calendarCurrentMonth.getMonth();
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);
        const daysInMonth = lastDay.getDate();
        const startingDayOfWeek = firstDay.getDay();
        
        let html = '';
        const dayHeaders = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
        dayHeaders.forEach(day => {
            html += `<div class="calendar-day-header">${day}</div>`;
        });
        
        for (let i = 0; i < startingDayOfWeek; i++) {
            html += '<div class="calendar-day empty"></div>';
        }
        
        for (let day = 1; day <= daysInMonth; day++) {
            const date = new Date(year, month, day);
            const dayOfWeek = date.getDay();
            
            const hasSchedule = schedules && schedules.length > 0 && schedules.some(s => s.dayOfWeek === dayOfWeek);
            
            const dateString = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
            const hasTimeOff = timeOffs && timeOffs.length > 0 && timeOffs.some(t => t.date && t.date.startsWith(dateString));
            
            let className = 'calendar-day';
            if (hasTimeOff) {
                className += ' has-timeoff';
            } else if (hasSchedule) {
                className += ' has-schedule';
            }
            
            html += `<div class="${className}" data-day="${day}">
                <div class="day-number">${day}</div>
            </div>`;
        }
        
        calendar.innerHTML = html;
    }
    
    // Make updateCalendarDisplay global for navigation buttons
    window.updateCalendarDisplay = updateCalendarDisplay;
    
    // Initialize calendar
    updateCalendarDisplay();
    
    // Edit Schedule Form Submit
    const editScheduleForm = document.getElementById('editScheduleForm');
    if (editScheduleForm) {
        // Clear errors on input
        ['editStartTime', 'editEndTime', 'editBookingLimit'].forEach(id => {
            const input = document.getElementById(id);
            if (input) {
                input.addEventListener('input', () => clearError(id));
                input.addEventListener('change', () => clearError(id));
            }
        });
        
        editScheduleForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            clearAllErrors('editScheduleForm');
            
            if (!validateEditScheduleForm()) {
                return;
            }
            
            const scheduleId = document.getElementById('editScheduleId').value;
            const startTime = document.getElementById('editStartTime').value;
            const endTime = document.getElementById('editEndTime').value;
            const bookingLimit = document.getElementById('editBookingLimit').value;
            
            if (!scheduleId) {
                showToast('error', 'Lỗi hệ thống', 'Không tìm thấy ID lịch làm việc');
                return;
            }
            
            const data = {
                startTime: startTime,
                endTime: endTime,
                bookingLimit: bookingLimit ? parseInt(bookingLimit) : null
            };
            
            try {
                setLoading(true);
                showToast('info', 'Đang xử lý', 'Đang cập nhật lịch làm việc...');
                
                const response = await fetch(`/Provider/ProviderSchedule/schedules/${scheduleId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                setLoading(false);
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã cập nhật lịch làm việc thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('editScheduleModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    setLoading(false);
                    // Đóng modal trước khi hiển thị lỗi
                    const editModal = bootstrap.Modal.getInstance(document.getElementById('editScheduleModal'));
                    if (editModal) {
                        editModal.hide();
                    }
                    // Đợi modal đóng xong rồi mới hiển thị thông báo
                    setTimeout(() => {
                        const errorMsg = translateError(result.message) || 'Đã xảy ra lỗi khi cập nhật lịch làm việc';
                        showToast('error', 'Không thể cập nhật', errorMsg);
                    }, 300);
                }
            } catch (error) {
                setLoading(false);
                // Đóng modal trước khi hiển thị lỗi
                const editModal = bootstrap.Modal.getInstance(document.getElementById('editScheduleModal'));
                if (editModal) {
                    editModal.hide();
                }
                // Đợi modal đóng xong rồi mới hiển thị thông báo
                setTimeout(() => {
                    showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
                }, 300);
            }
        });
    }
    
    // Weekly Form Submit
    const weeklyForm = document.getElementById('weeklyForm');
    if (weeklyForm) {
        // Clear errors on input
        ['weeklyStartTime', 'weeklyEndTime', 'weeklyBookingLimit'].forEach(id => {
            const input = document.getElementById(id);
            if (input) {
                input.addEventListener('input', () => clearError(id));
                input.addEventListener('change', () => clearError(id));
            }
        });
        
        // Clear days error when checkbox changes
        for (let i = 0; i <= 6; i++) {
            const checkbox = document.getElementById('day' + i);
            if (checkbox) {
                checkbox.addEventListener('change', () => clearError('days'));
            }
        }
        
        weeklyForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            clearAllErrors('weeklyForm');
            
            if (!validateWeeklyForm()) {
                return;
            }
            
            const daysSelected = [];
            for (let i = 0; i <= 6; i++) {
                if (document.getElementById('day' + i).checked) {
                    daysSelected.push(i);
                }
            }
            
            const startTime = document.getElementById('weeklyStartTime').value;
            const endTime = document.getElementById('weeklyEndTime').value;
            const bookingLimit = document.getElementById('weeklyBookingLimit').value;
            
            const data = {
                daysOfWeek: daysSelected,
                startTime: startTime,
                endTime: endTime,
                bookingLimit: bookingLimit ? parseInt(bookingLimit) : null
            };
            
            try {
                setLoading(true);
                const url = window.createWeeklyScheduleUrl;
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                setLoading(false);
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã tạo lịch làm việc theo tuần thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('createWeeklyModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    // Đóng modal trước khi hiển thị lỗi
                    const weeklyModal = bootstrap.Modal.getInstance(document.getElementById('createWeeklyModal'));
                    if (weeklyModal) {
                        weeklyModal.hide();
                    }
                    // Đợi modal đóng xong rồi mới hiển thị thông báo
                    setTimeout(() => {
                        const errorMsg = translateError(result.message) || 'Đã xảy ra lỗi khi tạo lịch tuần';
                        showToast('error', 'Không thể tạo lịch', errorMsg);
                    }, 300);
                }
            } catch (error) {
                // Đóng modal trước khi hiển thị lỗi
                const weeklyModal = bootstrap.Modal.getInstance(document.getElementById('createWeeklyModal'));
                if (weeklyModal) {
                    weeklyModal.hide();
                }
                // Đợi modal đóng xong rồi mới hiển thị thông báo
                setTimeout(() => {
                    showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
                }, 300);
            }
        });
    }
    
    // Daily Form Submit
    const dailyForm = document.getElementById('dailyForm');
    if (dailyForm) {
        // Clear errors on input
        ['dailyDayOfWeek', 'dailyStartTime', 'dailyEndTime', 'dailyBookingLimit'].forEach(id => {
            const input = document.getElementById(id);
            if (input) {
                input.addEventListener('input', () => clearError(id));
                input.addEventListener('change', () => clearError(id));
            }
        });
        
        dailyForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            clearAllErrors('dailyForm');
            
            if (!validateDailyForm()) {
                return;
            }
            
            const dayOfWeek = parseInt(document.getElementById('dailyDayOfWeek').value);
            const startTime = document.getElementById('dailyStartTime').value;
            const endTime = document.getElementById('dailyEndTime').value;
            const bookingLimit = document.getElementById('dailyBookingLimit').value;
            
            const data = {
                dayOfWeek: dayOfWeek,
                startTime: startTime,
                endTime: endTime,
                bookingLimit: bookingLimit ? parseInt(bookingLimit) : null
            };
            
            try {
                setLoading(true);
                const url = window.createScheduleUrl;
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                setLoading(false);
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã thêm ngày làm việc thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('createDailyModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    // Đóng modal trước khi hiển thị lỗi
                    const dailyModal = bootstrap.Modal.getInstance(document.getElementById('createDailyModal'));
                    if (dailyModal) {
                        dailyModal.hide();
                    }
                    // Đợi modal đóng xong rồi mới hiển thị thông báo
                    setTimeout(() => {
                        const errorMsg = translateError(result.message) || 'Đã xảy ra lỗi khi thêm ngày làm việc';
                        showToast('error', 'Không thể thêm ngày', errorMsg);
                    }, 300);
                }
            } catch (error) {
                // Đóng modal trước khi hiển thị lỗi
                const dailyModal = bootstrap.Modal.getInstance(document.getElementById('createDailyModal'));
                if (dailyModal) {
                    dailyModal.hide();
                }
                // Đợi modal đóng xong rồi mới hiển thị thông báo
                setTimeout(() => {
                    showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
                }, 300);
            }
        });
    }
    
    // Time Off Form Submit
    const timeOffForm = document.getElementById('timeOffForm');
    if (timeOffForm) {
        // Clear errors on input
        ['timeOffDate', 'timeOffReason'].forEach(id => {
            const input = document.getElementById(id);
            if (input) {
                input.addEventListener('input', () => clearError(id));
                input.addEventListener('change', () => clearError(id));
            }
        });
        
        timeOffForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            clearAllErrors('timeOffForm');
            
            if (!validateTimeOffForm()) {
                return;
            }
            
            const date = document.getElementById('timeOffDate').value;
            const reason = document.getElementById('timeOffReason').value;
            
            const data = {
                date: date,
                startTime: null,
                endTime: null,
                reason: reason || null
            };
            
            try {
                setLoading(true);
                const url = window.createTimeOffUrl;
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                setLoading(false);
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã tạo ngày nghỉ thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('createTimeOffModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    // Đóng modal trước khi hiển thị lỗi
                    const timeOffModal = bootstrap.Modal.getInstance(document.getElementById('createTimeOffModal'));
                    if (timeOffModal) {
                        timeOffModal.hide();
                    }
                    // Đợi modal đóng xong rồi mới hiển thị thông báo
                    setTimeout(() => {
                        const errorMsg = translateError(result.message) || 'Đã xảy ra lỗi khi tạo ngày nghỉ';
                        showToast('error', 'Không thể tạo ngày nghỉ', errorMsg);
                    }, 300);
                }
            } catch (error) {
                // Đóng modal trước khi hiển thị lỗi
                const timeOffModal = bootstrap.Modal.getInstance(document.getElementById('createTimeOffModal'));
                if (timeOffModal) {
                    timeOffModal.hide();
                }
                // Đợi modal đóng xong rồi mới hiển thị thông báo
                setTimeout(() => {
                    showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
                }, 300);
            }
        });
    }
});
