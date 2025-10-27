using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Feedback;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderFeedbackController : Controller
    {
        private readonly ProviderFeedbackService _service;

        public ProviderFeedbackController(ProviderFeedbackService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var token = HttpContext.Session.GetString("JWToken");

            if (!Guid.TryParse(accountIdStr, out var accountId) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ProviderFeedbackViewModel model;
            try
            {
                model = await _service.GetFeedbacksAsync(accountId, token, ct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải phản hồi khách hàng: " + ex.Message;
                model = new ProviderFeedbackViewModel();
            }

            // Sắp xếp: nhiều feedback trước, tie-break theo tên
            model.ServiceFeedbacks = model.ServiceFeedbacks
                .OrderByDescending(s => s.TotalFeedbacks)
                .ThenBy(s => s.ServiceName)
                .ToList();

            // KHÔNG CẦN thiết: View đã tự tính servicesRated
            // var servicesRated = model.ServiceFeedbacks.Count(s => s.TotalFeedbacks > 0);
            // ViewBag.ServicesRated = servicesRated;

            ViewData["Title"] = "Phản hồi khách hàng";
            return View(model);
        }
    }
}
