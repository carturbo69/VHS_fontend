using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Feedback;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderFeedbackController : Controller
    {
        private readonly ProviderFeedbackService _service;

        public ProviderFeedbackController(ProviderFeedbackService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var token = HttpContext.Session.GetString("JWToken");

            if (!Guid.TryParse(accountIdStr, out var accountId) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ProviderFeedbackViewModel model;
            try
            {
                model = await _service.GetFeedbacksAsync(accountId, token, ct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải phản hồi khách hàng: " + ex.Message;
                model = new ProviderFeedbackViewModel();
            }

            model.ServiceFeedbacks = model.ServiceFeedbacks
                .OrderByDescending(s => s.TotalFeedbacks)
                .ThenBy(s => s.ServiceName)
                .ToList();

            ViewData["Title"] = "Phản hồi khách hàng";
            return View(model);
        }

        // ===================== GỬI PHẢN HỒI (AJAX) =====================
        // Gọi bằng JS: fetch('/Provider/ProviderFeedback/ReplyAjax', { method:'POST', headers:{'Content-Type':'application/json','RequestVerificationToken': token}, body: JSON.stringify({ reviewId, content }) })
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Produces("application/json")]
        //public async Task<IActionResult> ReplyAjax([FromBody] ProviderReplyRequestDto dto, CancellationToken ct)
        //{
        //    if (dto == null || dto.ReviewId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Content))
        //        return BadRequest(new { success = false, message = "Dữ liệu phản hồi không hợp lệ." });

        //    if (!TryGetSession(out var accountId, out var token, out var fail))
        //        return fail!; // trả về 401 -> yêu cầu đăng nhập

        //    try
        //    {
        //        var ok = await _service.SendReplyAsync(accountId, dto, token, ct);
        //        if (!ok)
        //            return BadRequest(new { success = false, message = "Không thể gửi phản hồi. Hãy kiểm tra quyền sở hữu dịch vụ / trạng thái review." });

        //        return Ok(new { success = true, message = "Đã gửi phản hồi." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        //    }
        //}

        // ===================== GỬI PHẢN HỒI (FORM POST) =====================
        // Dùng cho <form method="post" asp-action="Reply"> (multipart/x-www-form-urlencoded)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply([FromForm] ProviderReplyRequestDto dto, CancellationToken ct)
        {
            // 1) Validate form tối thiểu
            if (dto == null || dto.ReviewId == Guid.Empty)
            {
                TempData["Error"] = "Dữ liệu phản hồi không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var content = (dto.Content ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Vui lòng nhập nội dung phản hồi.";
                return RedirectToAction(nameof(Index));
            }

            const int MAX_LEN = 2000;
            if (content.Length > MAX_LEN) content = content.Substring(0, MAX_LEN);
            dto.Content = content;

            // 2) Lấy session (đúng format với Index)
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var token = HttpContext.Session.GetString("JWToken");

            if (!Guid.TryParse(accountIdStr, out var accountId) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 3) Gọi service gửi phản hồi
            try
            {
                var ok = await _service.SendReplyAsync(accountId, dto, token, ct);
                if (!ok)
                {
                    TempData["Error"] = "Không thể gửi phản hồi. Hãy kiểm tra quyền sở hữu dịch vụ hoặc review đã được trả lời.";
                }
                else
                {
                    TempData["Success"] = "Đã gửi phản hồi.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            // 4) Quay lại danh sách
            return RedirectToAction(nameof(Index));
        }


        // ===================== Helper lấy Session =====================
        private bool TryGetSession(out Guid accountId, out string token, out IActionResult? failResult)
        {
            accountId = Guid.Empty;
            token = string.Empty;
            failResult = null;

            var accountIdStr = HttpContext.Session.GetString("AccountID");
            token = HttpContext.Session.GetString("JWToken") ?? string.Empty;

            if (!Guid.TryParse(accountIdStr, out accountId) || string.IsNullOrWhiteSpace(token))
            {
                // Nếu muốn trả JSON khi gọi AJAX:
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    failResult = Unauthorized(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
                }
                else
                {
                    failResult = RedirectToAction("Login", "Account", new { area = "" });
                }
                return false;
            }
            return true;
        }
    }
}
