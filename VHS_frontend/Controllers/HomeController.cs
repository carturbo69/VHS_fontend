using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models;
using VHS_frontend.Services.Admin;
using VHS_frontend.Areas.Admin.Models.Feedback;
using VHS_frontend.Services.Customer.Interfaces;
using VHS_frontend.Models.ServiceDTOs;

namespace VHS_frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly CustomerAdminService _customerService;
        private readonly ProviderAdminService _providerService;
        private readonly AdminFeedbackService _feedbackService;
        private readonly IServiceCustomerService _serviceCustomerService;

        public HomeController(
            CustomerAdminService customerService,
            ProviderAdminService providerService,
            AdminFeedbackService feedbackService,
            IServiceCustomerService serviceCustomerService)
        {
            _customerService = customerService;
            _providerService = providerService;
            _feedbackService = feedbackService;
            _serviceCustomerService = serviceCustomerService;
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

            // Lấy dịch vụ nổi bật (>= 4 sao), random và lấy 4 cái
            List<ListServiceHomePageDTOs> featuredServices = new List<ListServiceHomePageDTOs>();
            try
            {
                var allServices = await _serviceCustomerService.GetAllServiceHomePageAsync();
                var highRatedServices = allServices
                    .Where(s => s != null && s.AverageRating >= 4.0)
                    .ToList();

                if (highRatedServices.Any())
                {
                    // Nếu có >= 4 dịch vụ, random và lấy 4 cái
                    if (highRatedServices.Count >= 4)
                    {
                        var random = new Random((int)DateTime.Now.Ticks);
                        featuredServices = highRatedServices
                            .OrderBy(x => random.Next())
                            .Take(4)
                            .ToList();
                    }
                    else
                    {
                        // Nếu có < 4 dịch vụ, lấy tất cả
                        featuredServices = highRatedServices;
                    }
                }
                // Nếu không có dịch vụ nào >= 4 sao, featuredServices sẽ là list rỗng
            }
            catch { }

            ViewBag.FeaturedServices = featuredServices;

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
