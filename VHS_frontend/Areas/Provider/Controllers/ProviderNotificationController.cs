// Areas/Provider/Controllers/ProviderNotificationController.cs
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Notification;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderNotificationController : Controller
    {
        private readonly ProviderNotificationService _svc;
        public ProviderNotificationController(ProviderNotificationService svc) => _svc = svc;

        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token)) _svc.SetBearerToken(token);
        }

        // GET: Provider/ProviderNotification/Index - Trang danh sách thông báo
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var (items, total, unreadCount) = await _svc.GetMyNotificationsAsync(ct);

            ViewBag.Total = total;
            ViewBag.Unread = unreadCount;
            ViewBag.Read = total - unreadCount;
            return View(items);
        }

        // GET: Provider/ProviderNotification/Get/{id} - API lấy chi tiết thông báo
        [HttpGet]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var dto = await _svc.GetAsync(id, ct);
            return dto == null ? NotFound(new { message = "Không tìm thấy thông báo." }) : Ok(dto);
        }

        // DELETE: Provider/ProviderNotification/Delete/{id} - Xóa một thông báo
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            try
            {
                AttachBearerIfAny();
                var ok = await _svc.DeleteAsync(id, ct);
                if (ok)
                {
                    return Json(new { success = true, message = "Đã xóa thông báo" });
                }
                return Json(new { success = false, message = "Không thể xóa thông báo" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting notification: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa thông báo" });
            }
        }

        // POST: Provider/ProviderNotification/MarkAsRead/{id} - Đánh dấu đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
        {
            try
            {
                AttachBearerIfAny();
                var result = await _svc.MarkAsReadAsync(id, ct);
                // Parse result để kiểm tra success
                if (result != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(result);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var successElement))
                    {
                        var success = successElement.GetBoolean();
                        if (success)
                        {
                            return Json(new { success = true, message = "Đã đánh dấu đã đọc" });
                        }
                    }
                }
                return Json(new { success = false, message = "Không thể đánh dấu đã đọc" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking as read: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đánh dấu đã đọc" });
            }
        }

        // POST: Provider/ProviderNotification/MarkAllAsRead - Đánh dấu tất cả đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
        {
            try
            {
                AttachBearerIfAny();
                var result = await _svc.MarkAllAsReadAsync(ct);
                // Parse result để kiểm tra success
                if (result != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(result);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var successElement))
                    {
                        var success = successElement.GetBoolean();
                        if (success)
                        {
                            return Json(new { success = true, message = "Đã đánh dấu tất cả đã đọc" });
                        }
                    }
                }
                return Json(new { success = false, message = "Không thể đánh dấu tất cả đã đọc" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all as read: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đánh dấu tất cả đã đọc" });
            }
        }

        // GET: Provider/ProviderNotification/GetUnreadCount - API cho chuông thông báo
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                AttachBearerIfAny();
                var count = await _svc.GetUnreadCountAsync();
                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        // GET: Provider/ProviderNotification/GetNotificationsPartial - Partial view cho dropdown
        [HttpGet]
        public async Task<IActionResult> GetNotificationsPartial()
        {
            try
            {
                AttachBearerIfAny();
                var notifications = await _svc.GetUnreadNotificationsAsync();
                
                if (notifications == null || !notifications.Any())
                {
                    return PartialView("_ProviderNotificationDropdown", new List<ProviderNotificationDTO>());
                }
                
                return PartialView("_ProviderNotificationDropdown", notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading notifications: {ex.Message}");
                return PartialView("_ProviderNotificationDropdown", new List<ProviderNotificationDTO>());
            }
        }
    }
}

