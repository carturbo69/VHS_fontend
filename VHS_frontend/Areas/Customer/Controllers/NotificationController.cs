using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Customer.Models.Notification;
using VHS_frontend.Services.Customer;

// Class để quản lý dữ liệu ảo cho testing
public static class MockNotificationData
{
    private static List<NotificationDTO> _notifications = new List<NotificationDTO>
    {
        new NotificationDTO
        {
            NotificationId = "1",
            Content = "Đơn hàng #12345 đã được xác nhận",
            NotificationType = "Booking",
            IsRead = false,
            CreatedAt = DateTime.Now.AddHours(-2),
            RelatedId = "12345"
        },
        new NotificationDTO
        {
            NotificationId = "2",
            Content = "Dịch vụ vệ sinh nhà đã hoàn thành",
            NotificationType = "Booking",
            IsRead = false,
            CreatedAt = DateTime.Now.AddHours(-5),
            RelatedId = "12346"
        },
        new NotificationDTO
        {
            NotificationId = "3",
            Content = "Thanh toán thành công cho đơn hàng #12346",
            NotificationType = "Payment",
            IsRead = true,
            CreatedAt = DateTime.Now.AddDays(-1),
            RelatedId = "12346"
        },
        new NotificationDTO
        {
            NotificationId = "4",
            Content = "Đánh giá dịch vụ từ khách hàng",
            NotificationType = "System",
            IsRead = false,
            CreatedAt = DateTime.Now.AddDays(-2),
            RelatedId = "12347"
        },
        new NotificationDTO
        {
            NotificationId = "5",
            Content = "Đơn hàng #12348 đã được thanh toán",
            NotificationType = "Payment",
            IsRead = true,
            CreatedAt = DateTime.Now.AddDays(-3),
            RelatedId = "12348"
        }
    };

    public static List<NotificationDTO> GetAllNotifications()
    {
        return _notifications.ToList();
    }

    public static List<NotificationDTO> GetUnreadNotifications()
    {
        return _notifications.Where(n => !n.IsRead).ToList();
    }

    public static int GetUnreadCount()
    {
        return _notifications.Count(n => !n.IsRead);
    }

    public static bool MarkAsRead(string id)
    {
        var notification = _notifications.FirstOrDefault(n => n.NotificationId == id);
        if (notification != null)
        {
            notification.IsRead = true;
            return true;
        }
        return false;
    }

    public static bool MarkAllAsRead()
    {
        foreach (var notification in _notifications)
        {
            notification.IsRead = true;
        }
        return true;
    }

    public static bool DeleteNotification(string id)
    {
        Console.WriteLine($"MockNotificationData.DeleteNotification called with ID: {id}");
        Console.WriteLine($"Current notifications count: {_notifications.Count}");
        Console.WriteLine($"Current notification IDs: {string.Join(", ", _notifications.Select(n => n.NotificationId))}");
        
        var notification = _notifications.FirstOrDefault(n => n.NotificationId == id);
        if (notification != null)
        {
            Console.WriteLine($"Found notification to delete: {notification.NotificationId} - {notification.Content}");
            _notifications.Remove(notification);
            Console.WriteLine($"Notification removed. New count: {_notifications.Count}");
            return true;
        }
        else
        {
            Console.WriteLine($"Notification with ID {id} not found!");
            return false;
        }
    }

    public static bool ClearAllNotifications()
    {
        _notifications.Clear();
        return true;
    }

    public static void ResetData()
    {
        _notifications = new List<NotificationDTO>
        {
            new NotificationDTO
            {
                NotificationId = "1",
                Content = "Đơn hàng #12345 đã được xác nhận",
                NotificationType = "Booking",
                IsRead = false,
                CreatedAt = DateTime.Now.AddHours(-2),
                RelatedId = "12345"
            },
            new NotificationDTO
            {
                NotificationId = "2",
                Content = "Dịch vụ vệ sinh nhà đã hoàn thành",
                NotificationType = "Booking",
                IsRead = false,
                CreatedAt = DateTime.Now.AddHours(-5),
                RelatedId = "12346"
            },
            new NotificationDTO
            {
                NotificationId = "3",
                Content = "Thanh toán thành công cho đơn hàng #12346",
                NotificationType = "Payment",
                IsRead = true,
                CreatedAt = DateTime.Now.AddDays(-1),
                RelatedId = "12346"
            },
            new NotificationDTO
            {
                NotificationId = "4",
                Content = "Đánh giá dịch vụ từ khách hàng",
                NotificationType = "System",
                IsRead = false,
                CreatedAt = DateTime.Now.AddDays(-2),
                RelatedId = "12347"
            },
            new NotificationDTO
            {
                NotificationId = "5",
                Content = "Đơn hàng #12348 đã được thanh toán",
                NotificationType = "Payment",
                IsRead = true,
                CreatedAt = DateTime.Now.AddDays(-3),
                RelatedId = "12348"
            }
        };
    }
}

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: Customer/Notification
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông báo";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Sử dụng dữ liệu ảo từ MockNotificationData
            var mockNotifications = MockNotificationData.GetAllNotifications();
            
            var response = new NotificationListResponse
                {
                    Success = true,
                Message = "Lấy danh sách thông báo thành công",
                Data = mockNotifications,
                TotalCount = mockNotifications.Count,
                UnreadCount = mockNotifications.Count(n => !n.IsRead)
            };
            
            return View(response);
        }

        // GET: Customer/Notification/Unread
        public async Task<IActionResult> Unread()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // Sử dụng dữ liệu ảo từ MockNotificationData
            var unreadNotifications = MockNotificationData.GetUnreadNotifications();

            var response = new NotificationListResponse
            {
                Success = true,
                Message = "Lấy thông báo chưa đọc thành công",
                Data = unreadNotifications,
                TotalCount = MockNotificationData.GetAllNotifications().Count,
                UnreadCount = unreadNotifications.Count
            };

            return Json(response);
        }

        // GET: Customer/Notification/Detail/{id}
        public async Task<IActionResult> Detail(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông báo";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var response = await _notificationService.GetNotificationDetailAsync(id, token);
            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("Index");
            }

            return View(response.Data);
        }

        // PUT: Customer/Notification/MarkRead/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // Sử dụng MockNotificationData để đánh dấu đã đọc
            var success = MockNotificationData.MarkAsRead(id);
            if (success)
            {
                return Json(new { success = true, message = "Đã đánh dấu đã đọc" });
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy thông báo" });
            }
        }

        // PUT: Customer/Notification/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // Sử dụng MockNotificationData để đánh dấu tất cả đã đọc
            var success = MockNotificationData.MarkAllAsRead();
            if (success)
            {
                return Json(new { success = true, message = "Đã đánh dấu tất cả đã đọc" });
            }
            else
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // DELETE: Customer/Notification/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // Debug logging
            Console.WriteLine($"Attempting to delete notification with ID: {id}");
            var allNotifications = MockNotificationData.GetAllNotifications();
            Console.WriteLine($"Available notification IDs: {string.Join(", ", allNotifications.Select(n => n.NotificationId))}");

            // Sử dụng MockNotificationData để xóa thông báo
            var success = MockNotificationData.DeleteNotification(id);
            Console.WriteLine($"Delete result: {success}");
            
            if (success)
            {
                return Json(new { success = true, message = "Đã xóa thông báo" });
            }
            else
            {
                return Json(new { success = false, message = $"Không tìm thấy thông báo với ID: {id}" });
            }
        }

        // DELETE: Customer/Notification/ClearAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // Sử dụng MockNotificationData để xóa tất cả thông báo
            var success = MockNotificationData.ClearAllNotifications();
            if (success)
            {
                return Json(new { success = true, message = "Đã xóa tất cả thông báo" });
            }
            else
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Customer/Notification/GetUnreadCount - API cho header
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { count = 0 });
            }

            // Sử dụng MockNotificationData để đếm thông báo chưa đọc
            var count = MockNotificationData.GetUnreadCount();
            return Json(new { count = count });
        }

        // GET: Customer/Notification/ResetData - Reset dữ liệu ảo cho testing
        [HttpGet]
        public IActionResult ResetData()
        {
            MockNotificationData.ResetData();
            return Json(new { success = true, message = "Đã reset dữ liệu ảo" });
        }

        // GET: Customer/Notification/GetNotificationsPartial - Partial view cho dropdown
        [HttpGet]
        public async Task<IActionResult> GetNotificationsPartial()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return PartialView("_NotificationDropdown", new NotificationListResponse());
            }

            var response = await _notificationService.GetUnreadNotificationsAsync(token);
            
            // Nếu API trả về lỗi hoặc không có dữ liệu, tạo response rỗng
            if (!response.Success || response.Data == null)
            {
                response = new NotificationListResponse
                {
                    Success = true,
                    Message = "Không có thông báo nào",
                    Data = new List<NotificationDTO>(),
                    TotalCount = 0,
                    UnreadCount = 0
                };
            }
            
            return PartialView("_NotificationDropdown", response);
        }
    }
}
