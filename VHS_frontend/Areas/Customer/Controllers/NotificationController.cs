using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Customer.Models.Notification;
using VHS_frontend.Services.Customer;

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

            // Sử dụng API thật để lấy danh sách thông báo
            var response = await _notificationService.GetNotificationsAsync(token);
            
            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
                return View(new NotificationListResponse 
                { 
                    Success = false, 
                    Message = response.Message,
                    Data = new List<NotificationDTO>(),
                    TotalCount = 0,
                    UnreadCount = 0
                });
            }
            
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

            // Sử dụng API thật để lấy thông báo chưa đọc
            var response = await _notificationService.GetUnreadNotificationsAsync(token);
            
            if (!response.Success)
            {
                return Json(new { success = false, message = response.Message });
            }

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

            // Sử dụng API thật để đánh dấu đã đọc
            var success = await _notificationService.MarkAsReadAsync(id, token);
            if (success)
            {
                return Json(new { success = true, message = "Đã đánh dấu đã đọc" });
            }
            else
            {
                return Json(new { success = false, message = "Không thể đánh dấu đã đọc" });
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

            // Sử dụng API thật để đánh dấu tất cả đã đọc
            var success = await _notificationService.MarkAllAsReadAsync(token);
            if (success)
            {
                return Json(new { success = true, message = "Đã đánh dấu tất cả đã đọc" });
            }
            else
            {
                return Json(new { success = false, message = "Không thể đánh dấu tất cả đã đọc" });
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

            // Sử dụng API thật để xóa thông báo
            var success = await _notificationService.DeleteNotificationAsync(id, token);
            
            if (success)
            {
                return Json(new { success = true, message = "Đã xóa thông báo" });
            }
            else
            {
                return Json(new { success = false, message = "Không thể xóa thông báo" });
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

            // Sử dụng API thật để xóa tất cả thông báo
            var success = await _notificationService.ClearAllNotificationsAsync(token);
            if (success)
            {
                return Json(new { success = true, message = "Đã xóa tất cả thông báo" });
            }
            else
            {
                return Json(new { success = false, message = "Không thể xóa tất cả thông báo" });
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

            // Sử dụng API thật để đếm thông báo chưa đọc
            var count = await _notificationService.GetUnreadCountAsync(token);
            return Json(new { count = count });
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
