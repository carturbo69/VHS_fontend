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
            // Set bearer nếu bạn chưa dùng AuthHeaderHandler
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                _svc.SetBearerToken(token);

            // 1) Kiểm tra user đã có Provider chưa
            var mine = await _svc.GetMyProviderAsync(ct);

            if (mine != null && !mine.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                // ĐÃ CÓ HỒ SƠ & KHÔNG PHẢI Rejected -> chặn đăng ký, đưa sang Success
                return RedirectToAction(nameof(Success), new { id = mine.ProviderId, stat = mine.Status });
            }

            // 2) Nếu Rejected hoặc chưa có hồ sơ -> cho đăng ký
            //    - nếu Rejected: pass ProviderId cũ để view post lên (update trên id cũ)
            if (mine != null && mine.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ExistingProviderId = mine.ProviderId;
                ViewBag.ExistingStatus = mine.Status;
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
        public async Task<IActionResult> RegisterFallback(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                _svc.SetBearerToken(token);

            var result = await _svc.RegisterAsync(Request, ct);

            if (!result.Success)
            {
                TempData["ApiError"] = $"Đăng ký thất bại: {result.Message}";
                return RedirectToAction(nameof(Register));
            }

            // Controller này đang dùng DTO UPPERCASE
            return RedirectToAction(nameof(Success), new
            {
                id = result.Response!.PROVIDERID,
                stat = result.Response!.STATUS
            });
        }

        [HttpGet]
        public IActionResult Success(Guid id, string? stat)
        {
            ViewBag.ProviderId = id;
            ViewBag.Status = stat ?? "Pending";
            return View();
        }
    }
}
