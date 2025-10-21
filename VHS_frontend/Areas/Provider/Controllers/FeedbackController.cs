using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models.Review;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class FeedbackController : Controller
    {
        private readonly ProviderReviewService _reviewService;
        private readonly ProviderProfileService _profileService;

        public FeedbackController(
            ProviderReviewService reviewService,
            ProviderProfileService profileService)
        {
            _reviewService = reviewService;
            _profileService = profileService;
        }

        // GET: Provider/Feedback
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy AccountId từ Session
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để tiếp tục";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Lấy ProviderId từ AccountId
                var token = HttpContext.Session.GetString("JWToken");
                var providerId = await _profileService.GetProviderIdByAccountAsync(accountId, token);

                if (string.IsNullOrEmpty(providerId))
                {
                    TempData["Error"] = "Không tìm thấy thông tin Provider";
                    return RedirectToAction("Index", "ProviderDashboard");
                }

                // Lưu ProviderId vào ViewBag để dùng trong View
                ViewBag.ProviderId = providerId;

                // Lấy danh sách reviews
                var reviews = await _reviewService.GetReviewsByProviderIdAsync(providerId, token);

                // Lấy thống kê
                var statistics = await _reviewService.GetReviewStatisticsAsync(providerId, token);
                ViewBag.Statistics = statistics;

                ViewData["Title"] = "Phản hồi khách hàng";
                return View(reviews);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Feedback/Index: {ex.Message}");
                TempData["Error"] = "Không thể tải danh sách đánh giá: " + ex.Message;
                return View(new List<ProviderReviewReadDTO>());
            }
        }

        // POST: Provider/Feedback/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(Guid reviewId, string reply)
        {
            try
            {
                // Lấy AccountId từ Session
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Lấy ProviderId
                var token = HttpContext.Session.GetString("JWToken");
                var providerId = await _profileService.GetProviderIdByAccountAsync(accountId, token);

                if (string.IsNullOrEmpty(providerId))
                {
                    return Json(new { success = false, message = "Không tìm thấy Provider" });
                }

                // Validate
                if (string.IsNullOrWhiteSpace(reply))
                {
                    return Json(new { success = false, message = "Nội dung phản hồi không được để trống" });
                }

                if (reply.Length > 1000)
                {
                    return Json(new { success = false, message = "Nội dung phản hồi không được vượt quá 1000 ký tự" });
                }

                // Gọi API
                var replyDto = new ReplyReviewDTO { Reply = reply };
                var response = await _reviewService.ReplyToReviewAsync(
                    reviewId.ToString(),
                    providerId,
                    replyDto,
                    token);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Phản hồi đã được gửi thành công!" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ERROR] Reply failed: {response.StatusCode} - {errorContent}");
                    return Json(new { success = false, message = "Không thể gửi phản hồi. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Feedback/Reply: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Provider/Feedback/Statistics (AJAX)
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var token = HttpContext.Session.GetString("JWToken");
                var providerId = await _profileService.GetProviderIdByAccountAsync(accountId, token);

                if (string.IsNullOrEmpty(providerId))
                {
                    return Json(new { success = false, message = "Không tìm thấy Provider" });
                }

                var statistics = await _reviewService.GetReviewStatisticsAsync(providerId, token);
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Feedback/Statistics: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

