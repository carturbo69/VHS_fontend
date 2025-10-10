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
            // Chỉ hiển thị Category còn hoạt động
            var cats = await _svc.GetCategoriesAsync(includeDeleted: false, ct: ct);

            ViewBag.Categories = cats
                .OrderBy(c => c.Name)
                .Select(c => new { Id = c.CategoryId, Name = c.Name })
                .ToList();

            // đọc thông báo lỗi (nếu redirect về)
            if (TempData["ApiError"] is string apiErr && !string.IsNullOrWhiteSpace(apiErr))
                ViewBag.ApiError = apiErr;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterFallback(CancellationToken ct)
        {
            // Lấy access token từ session (hoặc nơi bạn lưu)
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                _svc.SetBearerToken(token);
            }

            var result = await _svc.RegisterAsync(Request, ct);

            if (!result.Success)
            {
                TempData["ApiError"] = $"Đăng ký thất bại: {result.Message}";
                return RedirectToAction(nameof(Register));
            }

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
            return View(); // Areas/Customer/Views/RegisterProvider/Success.cshtml
        }
    }
}
