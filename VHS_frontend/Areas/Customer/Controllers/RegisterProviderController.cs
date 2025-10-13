using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class RegisterProviderController : Controller
    {
        private readonly RegisterProviderService _svc;

        public RegisterProviderController(RegisterProviderService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Register(CancellationToken ct)
        {
            // Lấy token
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account", new { area = "" });

            _svc.SetBearerToken(token);

            // 1) Kiểm tra hồ sơ gần nhất
            var mine = await _svc.GetMyProviderAsync(ct);
            if (mine != null && !mine.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                // Đang có Pending/Approved => chặn đăng ký lại
                return RedirectToAction(nameof(Success), new { id = mine.ProviderId, stat = mine.Status });
            }

            // 2) Nếu Rejected hoặc chưa có hồ sơ => cho đăng ký
            // LƯU Ý: KHÔNG truyền ProviderId ẩn xuống form nữa (Rejected sẽ tạo ProviderId mới)
            if (mine != null && mine.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ExistingStatus = mine.Status; // chỉ để hiển thị thông báo
            }

            // Chỉ hiển thị Category còn hoạt động
            var cats = await _svc.GetCategoriesAsync(includeDeleted: false, ct: ct);
            ViewBag.Categories = cats
                .OrderBy(c => c.Name)
                .Select(c => new { Id = c.CategoryId, Name = c.Name })
                .ToList();

            if (TempData["ApiError"] is string apiErr && !string.IsNullOrWhiteSpace(apiErr))
                ViewBag.ApiError = apiErr;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // (tùy) nếu upload nhiều ảnh lớn:
        [RequestSizeLimit(50_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000, ValueCountLimit = 4096, KeyLengthLimit = 4096)]
        public async Task<IActionResult> RegisterFallback(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JWToken");
            bool isAjax =
                string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (Request.Headers.Accept.ToString()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

            if (string.IsNullOrEmpty(token))
            {
                if (isAjax) return Ok(new { ok = false, message = "Bạn chưa đăng nhập." });
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            _svc.SetBearerToken(token);

            try
            {
                var result = await _svc.RegisterAsync(Request, ct);

                if (!result.Success)
                {
                    var msg = string.IsNullOrWhiteSpace(result.Message)
                        ? "Đăng ký thất bại. Vui lòng kiểm tra dữ liệu và thử lại."
                        : result.Message;

                    // AJAX: luôn trả 200 + payload rõ ràng
                    if (isAjax) return Ok(new { ok = false, message = msg, errors = result.Errors });

                    // Non-AJAX: quay lại form với banner lỗi
                    TempData["ApiError"] = msg;
                    return RedirectToAction(nameof(Register));
                }

                // Thành công: chuẩn hóa STAT để không bao giờ undefined
                var id = result.Response?.PROVIDERID ?? Guid.Empty;
                var stat = string.IsNullOrWhiteSpace(result.Response?.STATUS) ? "Pending" : result.Response!.STATUS;

                if (isAjax) return Ok(new { ok = true, providerId = id, status = stat });

                return RedirectToAction(nameof(Success), new { id, stat });
            }
            catch (Exception ex)
            {
                var msg = $"Đăng ký thất bại: {ex.Message}";
                if (isAjax) return Ok(new { ok = false, message = msg });

                TempData["ApiError"] = msg;
                return RedirectToAction(nameof(Register));
            }
        }

        [HttpGet]
        public IActionResult Success(Guid id, string? stat)
        {
            ViewBag.ProviderId = id;
            ViewBag.Status = string.IsNullOrWhiteSpace(stat) ? "Pending" : stat;
            return View();
        }
    }
}
