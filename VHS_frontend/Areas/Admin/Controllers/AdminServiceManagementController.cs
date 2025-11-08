using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection; // for ActivatorUtilitiesConstructor
using System.Linq;
using VHS_frontend.Services.Admin;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Admin.Models.Provider;
using VHS_frontend.Areas.Provider.Models.Service;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminServiceManagementController : Controller
    {
        private readonly ProviderAdminService _providerAdminService;
        private readonly ServiceManagementService _serviceManagementService;
        private readonly ProviderProfileService _providerProfileService;

        [ActivatorUtilitiesConstructor]
        public AdminServiceManagementController(
            ProviderAdminService providerAdminService,
            ServiceManagementService serviceManagementService,
            ProviderProfileService providerProfileService)
        {
            _providerAdminService = providerAdminService;
            _serviceManagementService = serviceManagementService;
            _providerProfileService = providerProfileService;
        }

        // GET: Admin/AdminServiceManagement
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token)) _providerAdminService.SetBearerToken(token);

            // Lấy toàn bộ nhà cung cấp (role = Provider)
            var providers = await _providerAdminService.GetAllAsync(includeDeleted: false);

            // Lấy ProviderName và đếm số lượng dịch vụ PendingUpdate cho mỗi provider
            if (!string.IsNullOrWhiteSpace(token) && providers != null && providers.Any())
            {
                foreach (var provider in providers)
                {
                    try
                    {
                        // Lấy ProviderProfile để lấy ProviderName
                        var profile = await _providerProfileService.GetProfileAsync(provider.Id.ToString(), token);
                        if (profile != null && !string.IsNullOrEmpty(profile.ProviderName))
                        {
                            provider.ProviderName = profile.ProviderName;
                        }
                        
                        // Lấy ProviderId từ AccountId
                        var providerId = await _providerProfileService.GetProviderIdByAccountAsync(provider.Id.ToString(), token);
                        if (!string.IsNullOrEmpty(providerId))
                        {
                            // Lấy tất cả dịch vụ của provider
                            var services = await _serviceManagementService.GetServicesByProviderAsync(providerId, token);
                            if (services != null)
                            {
                                // Đếm số lượng dịch vụ có Status = "Pending" hoặc "PendingUpdate" (cần admin duyệt)
                                provider.PendingUpdateCount = services.Count(s => s.Status == "Pending" || s.Status == "PendingUpdate");
                            }
                        }
                    }
                    catch
                    {
                        // Nếu có lỗi, để giá trị mặc định
                        provider.ProviderName = string.Empty;
                        provider.PendingUpdateCount = 0;
                    }
                }
                
                // Sắp xếp providers: ưu tiên những provider có dịch vụ cần duyệt (PendingUpdateCount > 0) lên đầu
                // Sau đó sắp xếp theo tên tài khoản
                providers = providers
                    .OrderByDescending(p => p.PendingUpdateCount > 0) // Có dịch vụ cần duyệt lên đầu (true > false)
                    .ThenByDescending(p => p.PendingUpdateCount) // Sắp xếp theo số lượng dịch vụ cần duyệt (nhiều hơn trước)
                    .ThenBy(p => p.AccountName) // Sau đó sắp xếp theo tên tài khoản (A-Z)
                    .ToList();
            }

            ViewData["Title"] = "Quản lý dịch vụ theo Nhà cung cấp";
            return View(providers);
        }

        // GET: Admin/AdminServiceManagement/Details/{providerId}
        public async Task<IActionResult> Details(Guid id, string? name = null)
        {
            var token = HttpContext.Session.GetString("JWToken");
            // id hiện là AccountId → cần đổi sang ProviderId để truy vấn dịch vụ
            var providerId = await _providerProfileService.GetProviderIdByAccountAsync(id.ToString(), token);
            if (string.IsNullOrEmpty(providerId))
            {
                ViewBag.ProviderId = id;
                ViewBag.ProviderName = name ?? "Nhà cung cấp";
                ViewData["Title"] = $"Dịch vụ của {ViewBag.ProviderName}";
                return View(new List<ServiceProviderReadDTO>());
            }

            var services = await _serviceManagementService.GetServicesByProviderAsync(providerId, token);
            // Hiển thị TẤT CẢ trạng thái để admin dễ kiểm soát
            services = services ?? new List<ServiceProviderReadDTO>();

            ViewBag.ProviderId = id;
            ViewBag.ProviderName = name ?? "Nhà cung cấp";
            ViewData["Title"] = $"Dịch vụ của {ViewBag.ProviderName}";
            return View(services);
        }
    }
}


