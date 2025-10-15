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
                if (isAjax) return StatusCode(401, new { ok = false, message = "Bạn chưa đăng nhập." });
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

                    // Log chi tiết để debug
                    Console.WriteLine($"Register failed - StatusCode: {result.StatusCode}, Message: {msg}");
                    Console.WriteLine($"Raw API Response: {result.Raw}");

                    // Phân loại lỗi BE để hiển thị thông báo phù hợp
                    if (result.Raw.Contains("CancellationToken") || result.Raw.Contains("store type mapping"))
                    {
                        Console.WriteLine("Detected CancellationToken mapping error - BE needs to fix entity model");
                        msg = "Lỗi hệ thống: BE cần kiểm tra Entity Framework model (không được có CancellationToken trong entity).";
                    }
                    else if (result.Raw.Contains("Invalid object name") || result.Raw.Contains("Providers"))
                    {
                        Console.WriteLine("Detected table name error - BE using wrong table name");
                        msg = "Lỗi hệ thống: BE đang dùng tên bảng sai (Providers thay vì Provider).";
                    }
                    else if (result.Raw.Contains("REFERENCE constraint") || result.Raw.Contains("FK__"))
                    {
                        Console.WriteLine("Detected foreign key constraint error - BE should create new Provider instead of deleting");
                        msg = "Lỗi hệ thống: BE nên tạo Provider mới thay vì xóa Provider cũ (tránh foreign key constraint).";
                    }
                    else if (result.Raw.Contains("duplicate key") || result.Raw.Contains("UQ__Provider"))
                    {
                        Console.WriteLine("Detected duplicate key error - BE should generate new ProviderId");
                        msg = "Lỗi hệ thống: BE nên tạo ProviderId mới (NEWID()) thay vì dùng lại ID cũ.";
                    }

                    if (isAjax) return StatusCode(result.StatusCode == 0 ? 400 : result.StatusCode, new { ok = false, message = msg, detail = result.Detail, errors = result.Errors, raw = result.Raw });

                    TempData["ApiError"] = msg;
                    return RedirectToAction(nameof(Register));
                }

                var id = result.Response?.PROVIDERID ?? Guid.Empty;
                var stat = string.IsNullOrWhiteSpace(result.Response?.STATUS) ? "Pending" : result.Response!.STATUS;

                // Clear cache để đảm bảo dữ liệu mới nhất
                _svc.ClearCache();

                if (isAjax) return Ok(new { ok = true, providerId = id, status = stat, refresh = true });

                return RedirectToAction(nameof(Success), new { id, stat });
            }
            catch (Exception ex)
            {
                var msg = $"Đăng ký thất bại: {ex.Message}";
                if (isAjax) return StatusCode(500, new { ok = false, message = msg });

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
