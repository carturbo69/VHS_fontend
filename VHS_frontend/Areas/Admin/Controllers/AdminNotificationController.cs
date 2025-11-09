// Areas/Admin/Controllers/AdminNotificationController.cs
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Notification;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminNotificationController : Controller
    {
        private readonly AdminNotificationService _svc;
        public AdminNotificationController(AdminNotificationService svc) => _svc = svc;

        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token)) _svc.SetBearerToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? keyword = null, string? role = null, string? notificationType = null, bool? isRead = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            
            // Lấy token trước khi vào background task
            var token = HttpContext.Session.GetString("JWToken");
            
            // Tự động xóa các thông báo đã gửi cũ hơn 7 ngày (từ thời điểm tạo)
            // Chạy cleanup trong background để không chặn response
            _ = Task.Run(async () =>
            {
                try
                {
                    // Sử dụng service hiện có với token đã được set
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        _svc.SetBearerToken(token);
                    }
                    var deletedCount = await _svc.DeleteOldSentNotificationsAsync(default);
                    if (deletedCount > 0)
                    {
                        Console.WriteLine($"Đã xóa {deletedCount} thông báo đã gửi cũ hơn 7 ngày (từ thời điểm tạo)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up old notifications: {ex.Message}");
                }
            });
            
            // Không await cleanup task để không chặn response
            // Cleanup sẽ chạy song song và hoàn thành sau khi trang đã được render
            
            var (items, total) = await _svc.GetListAsync(new AdminNotificationQuery
            {
                Keyword = keyword,
                Role = role,
                NotificationType = notificationType,
                IsRead = isRead,
                Page = page,
                PageSize = pageSize
            }, ct);

            ViewBag.Total = total;
            ViewBag.Unread = items.Count(x => x.IsRead != true);
            ViewBag.Read = items.Count(x => x.IsRead == true);
            ViewBag.Keyword = keyword ?? "";
            ViewBag.Role = role ?? "";
            ViewBag.NotificationType = notificationType ?? "";
            ViewBag.IsRead = isRead;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var dto = await _svc.GetAsync(id, ct);
            return dto == null ? NotFound(new { message = "Không tìm thấy thông báo." }) : Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminNotificationCreateDTO dto, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var created = await _svc.CreateAsync(dto, ct);
                return Created(nameof(Get), created);
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendToRole([FromBody] AdminNotificationSendToRoleDTO dto, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _svc.SendToRoleAsync(dto, ct);
                return Ok(result);
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var ok = await _svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound(new { message = "Không tìm thấy thông báo." });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            try
            {
                var result = await _svc.MarkAsReadAsync(id, ct);
                return Ok(result);
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
        {
            AttachBearerIfAny();
            try
            {
                var result = await _svc.MarkAllAsReadAsync(ct);
                return Ok(result);
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts(CancellationToken ct = default)
        {
            try
            {
                AttachBearerIfAny();
                var accounts = await _svc.GetAccountsAsync(ct);
                
                if (accounts == null || !accounts.Any())
                {
                    return Ok(new List<object>()); // Return empty list instead of error
                }
                
                return Ok(accounts);
            }
            catch (ApiBadRequestException br)
            {
                Console.WriteLine($"API Bad Request: {br.Message}");
                return BadRequest(new { message = br.Message });
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
                return BadRequest(new { message = $"Không thể kết nối đến API backend: {httpEx.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                return BadRequest(new { message = $"Lỗi khi lấy danh sách tài khoản: {ex.Message}" });
            }
        }

        // GET: Admin/AdminNotification/GetUnreadCount - API cho chuông thông báo
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

        // GET: Admin/AdminNotification/GetNotificationsPartial - Partial view cho dropdown
        [HttpGet]
        public async Task<IActionResult> GetNotificationsPartial()
        {
            try
            {
                AttachBearerIfAny();
                var notifications = await _svc.GetUnreadNotificationsAsync();
                
                if (notifications == null || !notifications.Any())
                {
                    return PartialView("_AdminNotificationDropdown", new List<AdminNotificationDTO>());
                }
                
                return PartialView("_AdminNotificationDropdown", notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading notifications: {ex.Message}");
                return PartialView("_AdminNotificationDropdown", new List<AdminNotificationDTO>());
            }
        }

        // POST: Admin/AdminNotification/MarkRead/{id} - Đánh dấu đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            try
            {
                AttachBearerIfAny();
                var result = await _svc.MarkAsReadAsync(id);
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

        // POST: Admin/AdminNotification/MarkAllRead - Đánh dấu tất cả đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                AttachBearerIfAny();
                var result = await _svc.MarkAllAsReadAsync();
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

        // POST: Admin/AdminNotification/Delete/{id} - Xóa 1 thông báo (cho dropdown chuông)
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                AttachBearerIfAny();
                var ok = await _svc.DeleteAsync(id);
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
    }
}
