using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models;
using VHS_frontend.Services.Admin;
using VHS_frontend.Areas.Admin.Models.Feedback;

namespace VHS_frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly CustomerAdminService _customerService;
        private readonly ProviderAdminService _providerService;
        private readonly AdminFeedbackService _feedbackService;

        public HomeController(
            CustomerAdminService customerService,
            ProviderAdminService providerService,
            AdminFeedbackService feedbackService)
        {
            _customerService = customerService;
            _providerService = providerService;
            _feedbackService = feedbackService;
        }

        public async Task<IActionResult> Index()
        {
            // Nếu có token (đã đăng nhập), set để gọi API bảo vệ; nếu không, cứ thử gọi - sẽ fallback 0
            // var token = HttpContext.Session.GetString("JWToken");
            // if (!string.IsNullOrWhiteSpace(token))
            // {
            //     _customerService.SetBearerToken(token);
            //     _providerService.SetBearerToken(token);
            //     _feedbackService.SetBearerToken(token);
            // }

            int totalAccounts = 0;
            int totalProviders = 0;
            double avgRating = 0;

            try
            {
                var customers = await _customerService.GetAllAsync(includeDeleted: false);
                totalAccounts = customers?.Count ?? 0;
            }
            catch { totalAccounts = 0; }

            try
            {
                var providers = await _providerService.GetAllAsync(includeDeleted: false);
                totalProviders = providers?.Count ?? 0;
            }
            catch { totalProviders = 0; }

            try
            {
                var feedbacks = await _feedbackService.GetAllAsync();
                var visible = feedbacks?.Where(f => !f.IsDeleted && f.IsVisible).ToList() ?? new List<FeedbackDTO>();
                avgRating = visible.Any() ? Math.Round(visible.Average(f => f.Rating), 1) : 0;
            }
            catch { avgRating = 0; }

            ViewBag.TotalAccounts = totalAccounts;
            ViewBag.TotalProviders = totalProviders;
            ViewBag.AverageRating = avgRating;

            return View();
        }

        public async Task<IActionResult> About()
        {
            int totalProviders = 0;
            double avgRating = 0;

            try
            {
                var providers = await _providerService.GetAllAsync(includeDeleted: false);
                totalProviders = providers?.Count ?? 0;
            }
            catch { totalProviders = 0; }

            try
            {
                var feedbacks = await _feedbackService.GetAllAsync();
                var visible = feedbacks?.Where(f => !f.IsDeleted && f.IsVisible).ToList() ?? new List<FeedbackDTO>();
                avgRating = visible.Any() ? Math.Round(visible.Average(f => f.Rating), 1) : 0;
            }
            catch { avgRating = 0; }

            ViewBag.TotalProviders = totalProviders;
            ViewBag.AverageRating = avgRating;

            return View();
        }

        //public IActionResult Contact() => View();

        public IActionResult Policy()
        {
            ViewData["Title"] = "Chính sách bảo mật";
            return View();
        }

        public IActionResult Terms()
        {
            ViewData["Title"] = "Điều khoản sử dụng";
            return View();
        }
    }
}
