using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminSettingsController : Controller
    {
        private readonly AdminSettingsService _settingsService;

        public AdminSettingsController(AdminSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // GET: Admin/AdminSettings/Index
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            _settingsService.SetBearerToken(token);

            try
            {
                // Lấy thời gian hủy mặc định
                var cancelMinutes = await _settingsService.GetAutoCancelMinutesAsync();
                ViewBag.AutoCancelMinutes = cancelMinutes;
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải cài đặt: {ex.Message}";
                ViewBag.AutoCancelMinutes = 30; // Mặc định
                return View();
            }
        }

        // POST: Admin/AdminSettings/UpdateAutoCancelMinutes
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateAutoCancelMinutes([FromBody] UpdateAutoCancelMinutesRequest request)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            }

            if (request.Minutes <= 0)
            {
                return Json(new { success = false, message = "Số phút phải lớn hơn 0." });
            }

            try
            {
                _settingsService.SetBearerToken(token);
                var success = await _settingsService.UpdateAutoCancelMinutesAsync(request.Minutes);
                
                if (success)
                {
                    return Json(new { 
                        success = true, 
                        message = $"Đã cập nhật thời gian hủy mặc định thành {request.Minutes} phút.",
                        minutes = request.Minutes
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật thời gian hủy." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    public class UpdateAutoCancelMinutesRequest
    {
        public int Minutes { get; set; }
    }
}

