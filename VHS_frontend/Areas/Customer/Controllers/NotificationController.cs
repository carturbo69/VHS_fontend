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

            var response = await _notificationService.GetNotificationsAsync(token);
            
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

            var response = await _notificationService.GetUnreadNotificationsAsync(token);
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
        public async Task<IActionResult> MarkRead(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var success = await _notificationService.MarkAsReadAsync(id, token);
            return Json(new { success = success, message = success ? "Đã đánh dấu đã đọc" : "Có lỗi xảy ra" });
        }

        // PUT: Customer/Notification/MarkAllRead
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var success = await _notificationService.MarkAllAsReadAsync(token);
            return Json(new { success = success, message = success ? "Đã đánh dấu tất cả đã đọc" : "Có lỗi xảy ra" });
        }

        // DELETE: Customer/Notification/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var success = await _notificationService.DeleteNotificationAsync(id, token);
            return Json(new { success = success, message = success ? "Đã xóa thông báo" : "Có lỗi xảy ra" });
        }

        // DELETE: Customer/Notification/ClearAll
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var success = await _notificationService.ClearAllNotificationsAsync(token);
            return Json(new { success = success, message = success ? "Đã xóa tất cả thông báo" : "Có lỗi xảy ra" });
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

            var response = await _notificationService.GetUnreadNotificationsAsync(token);
            var count = response.Success ? response.UnreadCount : 0;
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
