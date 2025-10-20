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
                return Ok(accounts);
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
            catch (HttpRequestException httpEx)
            {
                return BadRequest(new { message = $"Không thể kết nối đến API backend: {httpEx.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi lấy danh sách tài khoản: {ex.Message}" });
            }
        }
    }
}
