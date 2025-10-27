using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Feedback;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminFeedbackController : Controller
    {
        private readonly AdminFeedbackService _feedbackService;

        public AdminFeedbackController(AdminFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        public async Task<IActionResult> Index()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            // Kiểm tra Session đăng nhập và quyền
            if (string.IsNullOrEmpty(accountId) ||
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";

            // Gắn token xác thực
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _feedbackService.SetBearerToken(token);

            try
            {
                // ✅ Gọi API thực tế từ ReviewsController
                var feedbacks = await _feedbackService.GetAllAsync();
                return View(feedbacks);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải danh sách feedback: " + ex.Message;
                return View(new List<FeedbackDTO>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(token))
                    _feedbackService.SetBearerToken(token);

                var success = await _feedbackService.DeleteAsync(id);
                TempData[success ? "Success" : "Error"] =
                    success ? "Xóa feedback thành công!" : "Không thể xóa feedback!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa feedback: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Hide(Guid id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(token))
                    _feedbackService.SetBearerToken(token);

                var success = await _feedbackService.HideAsync(id);
                TempData[success ? "Success" : "Error"] =
                    success ? "Ẩn feedback thành công!" : "Không thể ẩn feedback!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi ẩn feedback: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
