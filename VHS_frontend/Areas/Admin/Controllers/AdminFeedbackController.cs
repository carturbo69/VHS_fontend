using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Feedback;
using VHS_frontend.Services.Admin;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminFeedbackController : Controller
    {
        private readonly AdminFeedbackService _feedbackService;
        private readonly ProviderAdminService _providerAdminService;
        private readonly ServiceManagementService _serviceManagementService;
        private readonly ProviderProfileService _providerProfileService;

        public AdminFeedbackController(
            AdminFeedbackService feedbackService,
            ProviderAdminService providerAdminService,
            ServiceManagementService serviceManagementService,
            ProviderProfileService providerProfileService)
        {
            _feedbackService = feedbackService;
            _providerAdminService = providerAdminService;
            _serviceManagementService = serviceManagementService;
            _providerProfileService = providerProfileService;
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
                
                // Lọc feedback chỉ hiển thị các dịch vụ có status "Active"
                // Lấy danh sách tất cả services Active từ tất cả providers
                var token = HttpContext.Session.GetString("JWToken");
                var activeServiceIds = new HashSet<Guid>();
                
                if (!string.IsNullOrWhiteSpace(token))
                {
                    try
                    {
                        _providerAdminService.SetBearerToken(token);
                        var providers = await _providerAdminService.GetAllAsync(includeDeleted: false);
                        
                        foreach (var provider in providers)
                        {
                            try
                            {
                                // Lấy ProviderId từ AccountId
                                var providerId = await _providerProfileService.GetProviderIdByAccountAsync(provider.Id.ToString(), token);
                                if (!string.IsNullOrEmpty(providerId))
                                {
                                    // Lấy tất cả dịch vụ của provider
                                    var services = await _serviceManagementService.GetServicesByProviderAsync(providerId, token);
                                    if (services != null)
                                    {
                                        // Chỉ lấy các dịch vụ có Status = "Active"
                                        foreach (var service in services.Where(s => string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (service.ServiceId != Guid.Empty)
                                            {
                                                activeServiceIds.Add(service.ServiceId);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Bỏ qua lỗi khi lấy services của một provider
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        // Nếu có lỗi khi lấy danh sách providers/services, vẫn hiển thị feedback nhưng không lọc
                    }
                }
                
                // Lọc feedback: chỉ giữ lại feedback của các dịch vụ Active
                // Nếu ServiceId là null hoặc không có trong danh sách Active, loại bỏ
                if (activeServiceIds.Any())
                {
                    feedbacks = feedbacks.Where(f => 
                        f.ServiceId.HasValue && activeServiceIds.Contains(f.ServiceId.Value)
                    ).ToList();
                }
                // Nếu không lấy được danh sách Active services (do lỗi), vẫn hiển thị tất cả feedback
                // để tránh mất dữ liệu khi có lỗi tạm thời
                
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
