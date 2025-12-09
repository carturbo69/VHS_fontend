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
                
                // Lấy giờ giới hạn đặt trước (MinHoursAhead)
                var minHoursAhead = await _settingsService.GetMinHoursAheadAsync();
                ViewBag.MinHoursAhead = minHoursAhead;
                
                // Lấy ngày giới hạn đặt trước (MaxDaysAhead)
                var maxDaysAhead = await _settingsService.GetMaxDaysAheadAsync();
                ViewBag.MaxDaysAhead = maxDaysAhead;
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải cài đặt: {ex.Message}";
                ViewBag.AutoCancelMinutes = 30; // Mặc định
                ViewBag.MinHoursAhead = 3; // Mặc định 3 giờ
                ViewBag.MaxDaysAhead = 15; // Mặc định 15 ngày
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

        // POST: Admin/AdminSettings/UpdateMinHoursAhead
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateMinHoursAhead([FromBody] UpdateMinHoursAheadRequest request)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            }

            if (request.Hours < 1)
            {
                return Json(new { success = false, message = "Số giờ phải lớn hơn hoặc bằng 1." });
            }

            try
            {
                _settingsService.SetBearerToken(token);
                var success = await _settingsService.UpdateMinHoursAheadAsync(request.Hours);
                
                if (success)
                {
                    return Json(new { 
                        success = true, 
                        message = $"Đã cập nhật giờ giới hạn đặt trước thành {request.Hours} giờ.",
                        hours = request.Hours
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật giờ giới hạn." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: Admin/AdminSettings/UpdateMaxDaysAhead
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateMaxDaysAhead([FromBody] UpdateMaxDaysAheadRequest request)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            }

            if (request.Days < 1)
            {
                return Json(new { success = false, message = "Số ngày phải lớn hơn hoặc bằng 1." });
            }

            try
            {
                _settingsService.SetBearerToken(token);
                var success = await _settingsService.UpdateMaxDaysAheadAsync(request.Days);
                
                if (success)
                {
                    return Json(new { 
                        success = true, 
                        message = $"Đã cập nhật ngày giới hạn đặt trước thành {request.Days} ngày.",
                        days = request.Days
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật ngày giới hạn." });
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

    public class UpdateMinHoursAheadRequest
    {
        public int Hours { get; set; }
    }

    public class UpdateMaxDaysAheadRequest
    {
        public int Days { get; set; }
    }
}

