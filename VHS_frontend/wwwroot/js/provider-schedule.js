/**
 * Provider Schedule Management
 * Handles schedule creation, editing, deletion with smart notifications
 */

// ============================================
// GLOBAL VARIABLES & FUNCTIONS (Outside document.ready)
// ============================================

let confirmCallback = null;

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
            ${message}
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
                    showToast('error', 'Lỗi', result.message || 'Không thể xóa lịch làm việc');
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
                    showToast('error', 'Lỗi', result.message || 'Không thể xóa ngày nghỉ');
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
        editScheduleForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const scheduleId = document.getElementById('editScheduleId').value;
            const startTime = document.getElementById('editStartTime').value;
            const endTime = document.getElementById('editEndTime').value;
            const bookingLimit = document.getElementById('editBookingLimit').value;
            
            if (!scheduleId || !startTime || !endTime) {
                showToast('error', 'Lỗi nhập liệu', 'Vui lòng điền đầy đủ thông tin!');
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
                    showToast('error', 'Không thể cập nhật', result.message || 'Đã xảy ra lỗi khi cập nhật lịch làm việc');
                }
            } catch (error) {
                setLoading(false);
                showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
            }
        });
    }
    
    // Weekly Form Submit
    const weeklyForm = document.getElementById('weeklyForm');
    if (weeklyForm) {
        weeklyForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const daysSelected = [];
            for (let i = 0; i <= 6; i++) {
                if (document.getElementById('day' + i).checked) {
                    daysSelected.push(i);
                }
            }
            
            if (daysSelected.length === 0) {
                showToast('error', 'Lỗi nhập liệu', 'Vui lòng chọn ít nhất một ngày trong tuần!');
                return;
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
                const url = window.createWeeklyScheduleUrl;
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã tạo lịch làm việc theo tuần thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('createWeeklyModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showToast('error', 'Không thể tạo lịch', result.message || 'Đã xảy ra lỗi khi tạo lịch tuần');
                }
            } catch (error) {
                showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
            }
        });
    }
    
    // Daily Form Submit
    const dailyForm = document.getElementById('dailyForm');
    if (dailyForm) {
        dailyForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const dayOfWeek = parseInt(document.getElementById('dailyDayOfWeek').value);
            const startTime = document.getElementById('dailyStartTime').value;
            const endTime = document.getElementById('dailyEndTime').value;
            const bookingLimit = document.getElementById('dailyBookingLimit').value;
            
            if (!dayOfWeek && dayOfWeek !== 0) {
                showToast('error', 'Lỗi nhập liệu', 'Vui lòng chọn thứ trong tuần!');
                return;
            }
            
            const data = {
                dayOfWeek: dayOfWeek,
                startTime: startTime,
                endTime: endTime,
                bookingLimit: bookingLimit ? parseInt(bookingLimit) : null
            };
            
            try {
                const url = window.createScheduleUrl;
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã thêm ngày làm việc thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('createDailyModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showToast('error', 'Không thể thêm ngày', result.message || 'Đã xảy ra lỗi khi thêm ngày làm việc');
                }
            } catch (error) {
                showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
            }
        });
    }
    
    // Time Off Form Submit
    const timeOffForm = document.getElementById('timeOffForm');
    if (timeOffForm) {
        timeOffForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const date = document.getElementById('timeOffDate').value;
            const reason = document.getElementById('timeOffReason').value;
            
            const data = {
                date: date,
                startTime: null,
                endTime: null,
                reason: reason || null
            };
            
            try {
                const url = window.createTimeOffUrl;
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });
                
                const result = await response.json();
                
                if (result.success) {
                    showToast('success', 'Thành công', 'Đã tạo ngày nghỉ thành công!');
                    bootstrap.Modal.getInstance(document.getElementById('createTimeOffModal')).hide();
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showToast('error', 'Không thể tạo ngày nghỉ', result.message || 'Đã xảy ra lỗi khi tạo ngày nghỉ');
                }
            } catch (error) {
                showToast('error', 'Lỗi hệ thống', 'Không thể kết nối đến server. Vui lòng thử lại sau.');
            }
        });
    }
});
