// ============================================
// NOTIFICATION SYSTEM JAVASCRIPT
// ============================================

class NotificationManager {
    constructor() {
        this.dropdown = null;
        this.isDropdownOpen = false;
        this.unreadCount = 0;
        this.refreshInterval = null;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadUnreadCount();
        this.startAutoRefresh();
    }

    setupEventListeners() {
        // Đợi DOM load xong
        setTimeout(() => {
            const notifyBtn = document.querySelector('.notify-btn');
            console.log('NotificationManager: Setting up event listeners, notifyBtn found:', !!notifyBtn);
            
            if (notifyBtn) {
                // Hover để hiển thị dropdown
                notifyBtn.addEventListener('mouseenter', (e) => {
                    console.log('NotificationManager: Bell hovered - showing dropdown');
                    this.showDropdown();
                });
                
                // Mouse leave để ẩn dropdown
                notifyBtn.addEventListener('mouseleave', (e) => {
                    console.log('NotificationManager: Bell mouse left - hiding dropdown');
                    this.hideDropdown();
                });
                
                // Click để chuyển trang (không prevent default)
                notifyBtn.addEventListener('click', (e) => {
                    console.log('NotificationManager: Bell clicked - navigating to notification page');
                    // Không prevent default, để link hoạt động bình thường
                });
            } else {
                console.error('NotificationManager: .notify-btn not found!');
            }
        }, 100);

        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (this.isDropdownOpen && !e.target.closest('.notification-dropdown') && !e.target.closest('.notify-btn')) {
                this.closeDropdown();
            }
        });

        // Mark all as read button in page
        document.addEventListener('click', (e) => {
            if (e.target.id === 'markAllReadBtn') {
                this.markAllAsRead();
            }
        });

        // Clear all button
        document.addEventListener('click', (e) => {
            if (e.target.id === 'clearAllBtn') {
                this.clearAllNotifications();
            }
        });

        // Individual notification actions
        document.addEventListener('click', (e) => {
            if (e.target.closest('.mark-read')) {
                const notificationId = e.target.closest('.mark-read').dataset.notificationId;
                this.markAsRead(notificationId);
            }
        });

        document.addEventListener('click', (e) => {
            if (e.target.closest('.delete')) {
                const notificationId = e.target.closest('.delete').dataset.notificationId;
                this.deleteNotification(notificationId);
            }
        });

        // Filter tabs
        document.addEventListener('click', (e) => {
            if (e.target.closest('.filter-tab')) {
                const filter = e.target.closest('.filter-tab').dataset.filter;
                this.filterNotifications(filter);
            }
        });

        // Notification item click
        document.addEventListener('click', (e) => {
            const notificationItem = e.target.closest('.notification-item');
            if (notificationItem && !e.target.closest('.action-btn')) {
                const notificationId = notificationItem.dataset.notificationId;
                if (notificationId) {
                    this.openNotificationDetail(notificationId);
                }
            }
        });
    }

    async loadUnreadCount() {
        try {
            console.log('NotificationManager: Loading unread count...');
            const response = await fetch('/Customer/Notification/GetUnreadCount');
            console.log('NotificationManager: Response status:', response.status);
            if (response.ok) {
                const data = await response.json();
                console.log('NotificationManager: Unread count data:', data);
                this.updateBadge(data.count || 0);
            } else {
                console.log('NotificationManager: Response not ok, setting badge to 0');
                this.updateBadge(0);
            }
        } catch (error) {
            console.error('NotificationManager: Error loading unread count:', error);
            this.updateBadge(0);
        }
    }

    async showDropdown() {
        console.log('NotificationManager: Showing dropdown...');
        
        // Sử dụng dropdown có sẵn trong HTML
        if (!this.dropdown) {
            this.dropdown = document.getElementById('notificationDropdown');
            console.log('NotificationManager: Using existing dropdown:', !!this.dropdown);
        }
        
        if (!this.dropdown) {
            console.error('NotificationManager: No dropdown found!');
            return;
        }
        
        // Show dropdown ngay lập tức với position fixed
        this.dropdown.style.position = 'fixed';
        this.dropdown.style.top = '80px';
        this.dropdown.style.right = '20px';
        this.dropdown.style.display = 'block';
        this.dropdown.style.opacity = '1';
        this.dropdown.style.visibility = 'visible';
        this.dropdown.style.transform = 'translateY(0)';
        this.dropdown.style.pointerEvents = 'auto';
        this.dropdown.style.zIndex = '999999';
        this.dropdown.classList.add('show');
        this.isDropdownOpen = true;
        
        console.log('NotificationManager: Dropdown shown immediately');
        console.log('NotificationManager: Dropdown position:', this.dropdown.style.position);
        console.log('NotificationManager: Dropdown top:', this.dropdown.style.top);
        console.log('NotificationManager: Dropdown right:', this.dropdown.style.right);
        console.log('NotificationManager: Dropdown zIndex:', this.dropdown.style.zIndex);
        
        // Load notifications async
        try {
            const response = await fetch('/Customer/Notification/GetNotificationsPartial');
            if (response.ok) {
                const html = await response.text();
                this.dropdown.innerHTML = html;
                console.log('NotificationManager: Notifications loaded');
            } else {
                this.dropdown.innerHTML = `
                    <div class="dropdown-header">
                        <h3>Thông báo</h3>
                    </div>
                    <div class="dropdown-content">
                        <div class="empty-dropdown">
                            <div class="empty-icon">
                                <i class="icon-bell-off"></i>
                            </div>
                            <p>Không có thông báo nào</p>
                        </div>
                    </div>
                `;
            }
        } catch (error) {
            console.error('Error loading notifications:', error);
            this.dropdown.innerHTML = `
                <div class="dropdown-header">
                    <h3>Thông báo</h3>
                </div>
                <div class="dropdown-content">
                    <div class="empty-dropdown">
                        <div class="empty-icon">
                            <i class="icon-bell-off"></i>
                        </div>
                        <p>Không thể tải thông báo</p>
                    </div>
                </div>
            `;
        }
    }

    hideDropdown() {
        // Delay để user có thể di chuyển mouse vào dropdown
        setTimeout(() => {
            if (this.isDropdownOpen && !this.isMouseOverDropdown()) {
                this.closeDropdown();
            }
        }, 300);
    }

    isMouseOverDropdown() {
        const dropdown = document.querySelector('.notification-dropdown');
        const notifyBtn = document.querySelector('.notify-btn');
        return (dropdown && dropdown.matches(':hover')) || (notifyBtn && notifyBtn.matches(':hover'));
    }

    closeDropdown() {
        if (this.dropdown) {
            console.log('NotificationManager: Closing dropdown...');
            this.dropdown.classList.remove('show');
            this.isDropdownOpen = false;
            console.log('NotificationManager: Dropdown closed');
        }
    }

    setupDropdownListeners() {
        // Mark all as read in dropdown
        const markAllBtn = this.dropdown.querySelector('#markAllReadDropdown');
        if (markAllBtn) {
            markAllBtn.addEventListener('click', () => {
                this.markAllAsRead();
            });
        }

        // Notification item clicks in dropdown
        const notificationItems = this.dropdown.querySelectorAll('.notification-item');
        notificationItems.forEach(item => {
            item.addEventListener('click', () => {
                const notificationId = item.dataset.notificationId;
                if (notificationId) {
                    this.openNotificationDetail(notificationId);
                }
            });
        });

        // Hover events cho dropdown
        this.dropdown.addEventListener('mouseenter', () => {
            console.log('NotificationManager: Mouse entered dropdown');
        });

        this.dropdown.addEventListener('mouseleave', () => {
            console.log('NotificationManager: Mouse left dropdown');
            this.hideDropdown();
        });
    }

    updateBadge(count) {
        console.log('NotificationManager: Updating badge with count:', count);
        this.unreadCount = count;
        const badge = document.querySelector('.notify-badge');
        console.log('NotificationManager: Badge element found:', !!badge);
        if (badge) {
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count.toString();
                badge.style.display = 'flex';
                console.log('NotificationManager: Badge shown with count:', badge.textContent);
            } else {
                badge.style.display = 'none';
                console.log('NotificationManager: Badge hidden');
            }
        } else {
            console.error('NotificationManager: .notify-badge not found!');
        }
    }

    async markAsRead(notificationId) {
        try {
            const response = await fetch(`/Customer/Notification/MarkRead/${notificationId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });
            
            const data = await response.json();
            
            if (data.success) {
                // Update UI
                const notificationItem = document.querySelector(`[data-notification-id="${notificationId}"]`);
                if (notificationItem) {
                    notificationItem.classList.remove('unread');
                    notificationItem.classList.add('read');
                    
                    // Remove unread indicator
                    const unreadIndicator = notificationItem.querySelector('.unread-indicator, .unread-dot');
                    if (unreadIndicator) {
                        unreadIndicator.remove();
                    }
                    
                    // Hide mark as read button
                    const markReadBtn = notificationItem.querySelector('.mark-read');
                    if (markReadBtn) {
                        markReadBtn.remove();
                    }
                }
                
                // Update badge count
                this.updateBadge(Math.max(0, this.unreadCount - 1));
                
                // Show success message
                this.showToast('Đã đánh dấu đã đọc', 'success');
            } else {
                this.showToast(data.message || 'Có lỗi xảy ra', 'error');
            }
        } catch (error) {
            console.error('Error marking as read:', error);
            this.showToast('Có lỗi xảy ra', 'error');
        }
    }

    async markAllAsRead() {
        try {
            const response = await fetch('/Customer/Notification/MarkAllRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });
            
            const data = await response.json();
            
            if (data.success) {
                // Update all notification items
                const notificationItems = document.querySelectorAll('.notification-item.unread');
                notificationItems.forEach(item => {
                    item.classList.remove('unread');
                    item.classList.add('read');
                    
                    // Remove unread indicators
                    const unreadIndicator = item.querySelector('.unread-indicator, .unread-dot');
                    if (unreadIndicator) {
                        unreadIndicator.remove();
                    }
                    
                    // Hide mark as read buttons
                    const markReadBtn = item.querySelector('.mark-read');
                    if (markReadBtn) {
                        markReadBtn.remove();
                    }
                });
                
                // Update badge
                this.updateBadge(0);
                
                // Close dropdown if open
                this.closeDropdown();
                
                this.showToast('Đã đánh dấu tất cả đã đọc', 'success');
            } else {
                this.showToast(data.message || 'Có lỗi xảy ra', 'error');
            }
        } catch (error) {
            console.error('Error marking all as read:', error);
            this.showToast('Có lỗi xảy ra', 'error');
        }
    }

    async deleteNotification(notificationId) {
        if (!confirm('Bạn có chắc chắn muốn xóa thông báo này?')) {
            return;
        }
        
        try {
            const response = await fetch(`/Customer/Notification/Delete/${notificationId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });
            
            const data = await response.json();
            
            if (data.success) {
                // Remove notification item from UI
                const notificationItem = document.querySelector(`[data-notification-id="${notificationId}"]`);
                if (notificationItem) {
                    notificationItem.remove();
                }
                
                // Update badge if it was unread
                if (notificationItem && notificationItem.classList.contains('unread')) {
                    this.updateBadge(Math.max(0, this.unreadCount - 1));
                }
                
                this.showToast('Đã xóa thông báo', 'success');
            } else {
                this.showToast(data.message || 'Có lỗi xảy ra', 'error');
            }
        } catch (error) {
            console.error('Error deleting notification:', error);
            this.showToast('Có lỗi xảy ra', 'error');
        }
    }

    async clearAllNotifications() {
        if (!confirm('Bạn có chắc chắn muốn xóa tất cả thông báo?')) {
            return;
        }
        
        try {
            const response = await fetch('/Customer/Notification/ClearAll', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });
            
            const data = await response.json();
            
            if (data.success) {
                // Reload page to show empty state
                window.location.reload();
            } else {
                this.showToast(data.message || 'Có lỗi xảy ra', 'error');
            }
        } catch (error) {
            console.error('Error clearing all notifications:', error);
            this.showToast('Có lỗi xảy ra', 'error');
        }
    }

    filterNotifications(filter) {
        // Update active tab
        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.classList.remove('active');
        });
        document.querySelector(`[data-filter="${filter}"]`).classList.add('active');
        
        // Filter notification items
        const notificationItems = document.querySelectorAll('.notification-item');
        notificationItems.forEach(item => {
            const itemType = item.dataset.type;
            
            switch (filter) {
                case 'all':
                    item.style.display = 'flex';
                    break;
                case 'unread':
                    item.style.display = item.classList.contains('unread') ? 'flex' : 'none';
                    break;
                case 'booking':
                case 'payment':
                    item.style.display = itemType === filter ? 'flex' : 'none';
                    break;
                default:
                    item.style.display = 'flex';
                    break;
            }
        });
    }

    openNotificationDetail(notificationId) {
        window.location.href = `/Customer/Notification/Detail/${notificationId}`;
    }

    startAutoRefresh() {
        // Refresh unread count every 30 seconds
        this.refreshInterval = setInterval(() => {
            this.loadUnreadCount();
        }, 30000);
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    showToast(message, type = 'info') {
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <span class="toast-message">${message}</span>
                <button class="toast-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;
        
        // Add to page
        document.body.appendChild(toast);
        
        // Show toast
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);
        
        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (toast.parentElement) {
                    toast.remove();
                }
            }, 300);
        }, 3000);
    }

    // Public methods for external use
    refresh() {
        this.loadUnreadCount();
        if (this.isDropdownOpen) {
            this.openDropdown();
        }
    }

    destroy() {
        this.stopAutoRefresh();
        if (this.dropdown) {
            this.dropdown.remove();
        }
    }
}

// Initialize notification manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('NotificationManager: DOM loaded, initializing...');
    window.notificationManager = new NotificationManager();
    console.log('NotificationManager: Initialized successfully');
    
    // Không cần test function nữa
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NotificationManager;
}

// Toast CSS (inject if not present)
if (!document.querySelector('#toast-styles')) {
    const toastStyles = document.createElement('style');
    toastStyles.id = 'toast-styles';
    toastStyles.textContent = `
        .toast {
            position: fixed;
            top: 20px;
            right: 20px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
            padding: 16px 20px;
            min-width: 300px;
            z-index: 10000;
            opacity: 0;
            transform: translateX(100%);
            transition: all 0.3s ease;
            border-left: 4px solid #3b82f6;
        }
        
        .toast.show {
            opacity: 1;
            transform: translateX(0);
        }
        
        .toast-success {
            border-left-color: #16a34a;
        }
        
        .toast-error {
            border-left-color: #dc2626;
        }
        
        .toast-warning {
            border-left-color: #d97706;
        }
        
        .toast-content {
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
        
        .toast-message {
            font-size: 14px;
            color: #1e293b;
            font-weight: 500;
        }
        
        .toast-close {
            background: none;
            border: none;
            font-size: 18px;
            color: #64748b;
            cursor: pointer;
            padding: 0;
            margin-left: 12px;
            width: 20px;
            height: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        
        .toast-close:hover {
            color: #1e293b;
        }
    `;
    document.head.appendChild(toastStyles);
}
