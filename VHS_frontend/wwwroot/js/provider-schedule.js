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
            'working hours must be between 07:00 and 17:00 during pilot period': 'Giờ làm việc phải trong khoảng từ 07:00 đến 17:00 trong giai đoạn thử nghiệm',
            'invalid start time format. expected hh:mm': 'Định dạng giờ bắt đầu không hợp lệ. Vui lòng nhập theo định dạng HH:mm',
            'invalid end time format. expected hh:mm': 'Định dạng giờ kết thúc không hợp lệ. Vui lòng nhập theo định dạng HH:mm',
            'start time must be before end time': 'Giờ bắt đầu phải nhỏ hơn giờ kết thúc',
            'schedule not found or does not belong to provider': 'Không tìm thấy lịch làm việc hoặc lịch không thuộc về nhà cung cấp này',
            'booking limit must be between 1 and 100': 'Giới hạn đơn phải từ 1 đến 100',
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
            'failed to update schedule': 'Không thể cập nhật lịch làm việc',
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
    
    // Chỉ validate booking limit (không validate giờ làm việc vì chỉ chỉnh sửa giới hạn đơn)
    const bookingLimitInput = document.getElementById('editBookingLimit').value;
    // Chỉ validate nếu có giá trị và không phải rỗng/null
    if (bookingLimitInput && bookingLimitInput.trim() !== '' && bookingLimitInput !== 'null') {
        const limit = parseInt(bookingLimitInput.trim());
        if (isNaN(limit) || limit < 1 || limit > 100) {
            showError('editBookingLimit', 'Giới hạn đơn phải từ 1 đến 100');
            isValid = false;
        } else {
            clearError('editBookingLimit');
        }
    } else {
        // Nếu rỗng hoặc null thì clear error (cho phép không giới hạn)
        clearError('editBookingLimit');
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

// Open Edit Schedule Modal from button (using data attributes)
window.openEditScheduleModalFromButton = function(button) {
    const scheduleId = button.getAttribute('data-schedule-id');
    const dayName = button.getAttribute('data-day-name');
    const startTime = button.getAttribute('data-start-time');
    const endTime = button.getAttribute('data-end-time');
    const bookingLimit = button.getAttribute('data-booking-limit');
    
    openEditScheduleModal(scheduleId, dayName, startTime, endTime, bookingLimit);
};

// Open Edit Schedule Modal (Global)
window.openEditScheduleModal = function(scheduleId, dayName, startTime, endTime, bookingLimit) {
    document.getElementById('editScheduleId').value = scheduleId;
    document.getElementById('editScheduleDayName').textContent = dayName;
    
    // Hiển thị giờ làm việc (read-only)
    document.getElementById('editStartTimeDisplay').value = startTime || '';
    document.getElementById('editEndTimeDisplay').value = endTime || '';
    document.getElementById('editStartTime').value = startTime || '';
    document.getElementById('editEndTime').value = endTime || '';
    
    // Xử lý bookingLimit: nếu là null, "null", undefined, hoặc rỗng thì để trống
    if (bookingLimit === null || bookingLimit === undefined || bookingLimit === 'null' || bookingLimit === '' || bookingLimit === 'undefined') {
        document.getElementById('editBookingLimit').value = '';
    } else {
        document.getElementById('editBookingLimit').value = bookingLimit;
    }
    
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
    
    // Debug: Log timeOffs data
    console.log('TimeOffs data:', timeOffs);
    if (timeOffs && timeOffs.length > 0) {
        console.log('First timeOff sample:', timeOffs[0]);
    }
    
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
        
        // Tạo map các ngày trong tuần có schedule
        const scheduleDays = new Set();
        if (schedules && Array.isArray(schedules) && schedules.length > 0) {
            schedules.forEach(s => {
                // Kiểm tra DayOfWeek (PascalCase từ C# model) trước, sau đó mới kiểm tra dayOfWeek (camelCase)
                const dayOfWeek = s.DayOfWeek !== undefined ? s.DayOfWeek : 
                                 (s.dayOfWeek !== undefined ? s.dayOfWeek : null);
                if (dayOfWeek !== null && dayOfWeek !== undefined) {
                    scheduleDays.add(parseInt(dayOfWeek));
                }
            });
        }
        
        for (let day = 1; day <= daysInMonth; day++) {
            const date = new Date(year, month, day);
            const dayOfWeek = date.getDay();
            
            const hasSchedule = scheduleDays.has(dayOfWeek);
            
            const dateString = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
            // Sử dụng window.timeOffsData thay vì biến local timeOffs
            const currentTimeOffs = window.timeOffsData || [];
            // Check both Date (PascalCase) and date (camelCase) properties
            // Also handle different date formats (with or without time)
            const hasTimeOff = currentTimeOffs && currentTimeOffs.length > 0 && currentTimeOffs.some(t => {
                const timeOffDate = t.Date || t.date; // Check both PascalCase and camelCase
                if (!timeOffDate) return false;
                // Convert to string and check if it matches the date string
                let dateStr = '';
                if (typeof timeOffDate === 'string') {
                    dateStr = timeOffDate.split('T')[0]; // Remove time part if exists
                    dateStr = dateStr.split(' ')[0]; // Remove time part if exists (space separator)
                } else if (timeOffDate instanceof Date) {
                    dateStr = timeOffDate.toISOString().split('T')[0];
                } else {
                    dateStr = String(timeOffDate).split('T')[0].split(' ')[0];
                }
                // So sánh chính xác với dateString (YYYY-MM-DD)
                const matches = dateStr === dateString;
                if (matches) {
                    console.log(`Time-off found for ${dateString}:`, t);
                }
                return matches;
            });
            
            let className = 'calendar-day';
            if (hasTimeOff) {
                className += ' has-timeoff';
            } else if (hasSchedule) {
                className += ' has-schedule';
            }
            
            html += `<div class="${className}" data-day="${day}" data-day-of-week="${dayOfWeek}">
                <div class="day-number">${day}</div>
            </div>`;
        }
        
        calendar.innerHTML = html;
        
        // Load time-offs từ API cho tháng hiện tại và cập nhật màu
        loadTimeOffsForMonth(firstDay.toISOString().split('T')[0], lastDay.toISOString().split('T')[0]);
    }
    
    // Function to load time-offs from API
    function loadTimeOffsForMonth(startDate, endDate) {
        fetch(`/Provider/ProviderSchedule/GetTimeOffs?startDate=${startDate}&endDate=${endDate}`)
            .then(async res => {
                const text = await res.text();
                try {
                    return JSON.parse(text);
                } catch (e) {
                    return { success: false, data: [] };
                }
            })
            .then(result => {
                const loadedTimeOffs = result.success ? result.data : (result.data || []);
                console.log('Loaded time-offs from API:', loadedTimeOffs);
                
                // Cập nhật window.timeOffsData với dữ liệu mới
                if (Array.isArray(loadedTimeOffs)) {
                    // Merge với dữ liệu hiện có, tránh trùng lặp
                    const existingTimeOffs = window.timeOffsData || [];
                    const mergedTimeOffs = [...existingTimeOffs];
                    
                    loadedTimeOffs.forEach(newTo => {
                        const timeOffDate = newTo.Date || newTo.date;
                        if (!timeOffDate) return;
                        
                        let dateStr = '';
                        if (typeof timeOffDate === 'string') {
                            dateStr = timeOffDate.split('T')[0];
                        } else if (timeOffDate instanceof Date) {
                            dateStr = timeOffDate.toISOString().split('T')[0];
                        } else {
                            dateStr = String(timeOffDate).split('T')[0];
                        }
                        
                        // Kiểm tra xem đã có chưa
                        const exists = mergedTimeOffs.some(existing => {
                            const existingDate = existing.Date || existing.date;
                            if (!existingDate) return false;
                            let existingDateStr = '';
                            if (typeof existingDate === 'string') {
                                existingDateStr = existingDate.split('T')[0];
                            } else if (existingDate instanceof Date) {
                                existingDateStr = existingDate.toISOString().split('T')[0];
                            } else {
                                existingDateStr = String(existingDate).split('T')[0];
                            }
                            return existingDateStr === dateStr;
                        });
                        
                        if (!exists) {
                            mergedTimeOffs.push(newTo);
                        }
                    });
                    
                    window.timeOffsData = mergedTimeOffs;
                }
                
                // Apply time-off colors to calendar days
                if (Array.isArray(loadedTimeOffs) && loadedTimeOffs.length > 0) {
                    console.log(`Applying time-off colors for ${loadedTimeOffs.length} time-offs`);
                    loadedTimeOffs.forEach(to => {
                        const timeOffDate = to.Date || to.date;
                        if (!timeOffDate) {
                            console.warn('Time-off missing date:', to);
                            return;
                        }
                        
                        // Convert to date string format YYYY-MM-DD
                        let dateStr = '';
                        if (typeof timeOffDate === 'string') {
                            dateStr = timeOffDate.split('T')[0].split(' ')[0]; // Remove time part if exists
                        } else if (timeOffDate instanceof Date) {
                            dateStr = timeOffDate.toISOString().split('T')[0];
                        } else {
                            dateStr = String(timeOffDate).split('T')[0].split(' ')[0];
                        }
                        
                        console.log(`Processing time-off date: ${dateStr} (original: ${timeOffDate})`);
                        
                        // Extract day number from date string (YYYY-MM-DD)
                        const dayMatch = dateStr.match(/(\d{4})-(\d{2})-(\d{2})/);
                        if (dayMatch) {
                            const day = parseInt(dayMatch[3]);
                            const month = parseInt(dayMatch[2]);
                            const year = parseInt(dayMatch[1]);
                            
                            // Verify this date is in the current calendar month
                            const currentYear = window.calendarCurrentMonth.getFullYear();
                            const currentMonth = window.calendarCurrentMonth.getMonth() + 1; // getMonth() returns 0-11
                            
                            if (year === currentYear && month === currentMonth) {
                                const dayElements = document.querySelectorAll(`[data-day="${day}"]`);
                                console.log(`Found ${dayElements.length} calendar day elements for day ${day}`);
                                
                                dayElements.forEach(el => {
                                    // Remove has-schedule class and add has-timeoff
                                    el.classList.remove('has-schedule');
                                    el.classList.add('has-timeoff');
                                    console.log(`Applied has-timeoff class to day ${day}`);
                                });
                            } else {
                                console.log(`Time-off date ${dateStr} is not in current month (${currentYear}-${currentMonth})`);
                            }
                        } else {
                            console.warn(`Could not parse date string: ${dateStr}`);
                        }
                    });
                } else {
                    console.log('No time-offs to apply');
                }
            })
            .catch(error => {
                console.error('Error loading time-offs:', error);
            });
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
            const bookingLimitInput = document.getElementById('editBookingLimit').value;
            
            if (!scheduleId) {
                showToast('error', 'Lỗi hệ thống', 'Không tìm thấy ID lịch làm việc');
                return;
            }
            
            // Xử lý bookingLimit: nếu rỗng, null, hoặc "null" thì gửi null, ngược lại parse thành số
            let bookingLimit = null;
            if (bookingLimitInput && bookingLimitInput.trim() !== '' && bookingLimitInput !== 'null') {
                const parsed = parseInt(bookingLimitInput.trim());
                if (!isNaN(parsed) && parsed > 0) {
                    bookingLimit = parsed;
                }
            }
            
            // Chỉ gửi BookingLimit, không gửi StartTime và EndTime (chỉ chỉnh sửa giới hạn đơn)
            const data = {
                BookingLimit: bookingLimit
            };
            
            console.log('Update Schedule Data:', data);
            console.log('Schedule ID:', scheduleId);
            
            try {
                setLoading(true);
                showToast('info', 'Đang xử lý', 'Đang cập nhật lịch làm việc...');
                
                const url = `/Provider/ProviderSchedule/UpdateSchedule/${scheduleId}`;
                console.log('Update URL:', url);
                
                const response = await fetch(url, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                console.log('Response Status:', response.status);
                
                const responseText = await response.text();
                console.log('Response Text:', responseText);
                
                let result;
                try {
                    result = JSON.parse(responseText);
                } catch (e) {
                    console.error('Failed to parse response as JSON:', e);
                    setLoading(false);
                    showToast('error', 'Lỗi hệ thống', 'Phản hồi từ server không hợp lệ');
                    return;
                }
                
                console.log('Parsed Result:', result);
                setLoading(false);
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã cập nhật giới hạn đơn thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('editScheduleModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    setLoading(false);
                    // Parse error message (có thể là JSON string)
                    let errorMsg = result.message || 'Đã xảy ra lỗi khi cập nhật giới hạn đơn';
                    try {
                        // Thử parse JSON nếu message là JSON string
                        const parsed = JSON.parse(errorMsg);
                        if (parsed.message) {
                            errorMsg = parsed.message;
                        }
                    } catch (e) {
                        // Không phải JSON, dùng message trực tiếp
                    }
                    
                    // Đóng modal trước khi hiển thị lỗi
                    const editModal = bootstrap.Modal.getInstance(document.getElementById('editScheduleModal'));
                    if (editModal) {
                        editModal.hide();
                    }
                    // Đợi modal đóng xong rồi mới hiển thị thông báo
                    setTimeout(() => {
                        const translatedMsg = translateError(errorMsg) || errorMsg;
                        showToast('error', 'Không thể cập nhật', translatedMsg);
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
