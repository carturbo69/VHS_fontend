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

        // Helper: kiểm tra quyền admin + gắn bearer nếu có
        private bool PrepareAuth()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) ||
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _feedbackService.SetBearerToken(token);

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            return true;
        }


        //[HttpGet]
        //public async Task<IActionResult> UnreadTotal(CancellationToken ct)
        //{
        //    // Ép login nếu chưa có AccountID
        //    if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

        //    var jwt = GetJwtFromRequest();

        //    var total = await _chatService.GetUnreadTotalAsync(
        //        accountId: myId,
        //        jwtToken: jwt,
        //        ct: ct
        //    );

        //    return Ok(new { total });
        //}


        public async Task<IActionResult> Index()
        {
            if (!PrepareAuth())
                return RedirectToAction("Login", "Account", new { area = "" });

            try
            {
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!PrepareAuth())
                return RedirectToAction("Login", "Account", new { area = "" });

            try
            {
                var success = await _feedbackService.DeleteAsync(id);
                TempData[success ? "Success" : "Error"] =
                    success ? "Xóa (mềm) feedback thành công!" : "Không thể xóa feedback!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa feedback: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(Guid id)
        {
            if (!PrepareAuth())
                return RedirectToAction("Login", "Account", new { area = "" });

            try
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Show(Guid id)
        {
            if (!PrepareAuth())
                return RedirectToAction("Login", "Account", new { area = "" });

            try
            {
                var success = await _feedbackService.ShowAsync(id);
                TempData[success ? "Success" : "Error"] =
                    success ? "Hiện feedback thành công!" : "Không thể hiện feedback!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi hiện feedback: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
