using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models;
using VHS_frontend.Services;
using System;

namespace VHS_frontend.Controllers
{
    public class ServiceShopController : Controller
    {
        private readonly ServiceShopService _serviceShopService;

        public ServiceShopController(ServiceShopService serviceShopService)
        {
            _serviceShopService = serviceShopService;
        }

        /// <summary>
        /// Hiển thị trang shop của provider
        /// </summary>
        public async Task<IActionResult> Index(Guid? providerId, int? categoryId, Guid? tagId, string sortBy = "popular", int page = 1)
        {
            Console.WriteLine($"[ServiceShopController] === Index START ===");
            Console.WriteLine($"[ServiceShopController] providerId: {providerId}");
            Console.WriteLine($"[ServiceShopController] categoryId: {categoryId}, tagId: {tagId}, sortBy: {sortBy}, page: {page}");
            System.Diagnostics.Debug.WriteLine($"=== ServiceShopController.Index START ===");
            System.Diagnostics.Debug.WriteLine($"providerId: {providerId}");
            System.Diagnostics.Debug.WriteLine($"categoryId: {categoryId}, tagId: {tagId}, sortBy: {sortBy}, page: {page}");

            // Validate providerId
            if (!providerId.HasValue || providerId.Value == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: providerId is null or empty");
                return BadRequest("Provider ID is required");
            }

            try
            {
                // Lấy ViewModel từ service
                var viewModel = await _serviceShopService.GetServiceShopViewModelAsync(
                    providerId.Value,
                    categoryId,
                    tagId,
                    sortBy,
                    page
                );

                if (viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: ViewModel is null for providerId {providerId.Value}");
                    // Trả về view với ViewModel trống thay vì NotFound
                    viewModel = new Models.ServiceShop.ServiceShopViewModel
                    {
                        ProviderId = providerId.Value,
                        ShopInfo = new Models.ServiceShop.ShopInfo
                        {
                            Id = providerId.Value.GetHashCode(),
                            Name = "Đối tác VHS",
                            Logo = "/images/vhs_logo.png",
                            Status = "Online",
                            LastOnline = "Gần đây",
                            TotalServices = 0,
                            ResponseRate = 100.0
                        },
                        Services = new List<ServiceItem>(),
                        BestsellingServices = new List<ServiceItem>(),
                        ShopCategories = new List<Models.ServiceShop.ServiceCategory>(),
                        AllCategories = new List<Models.ServiceShop.ServiceCategory>(),
                        CurrentPage = 1,
                        TotalPages = 1,
                        SelectedCategoryId = categoryId,
                        SelectedTagId = tagId,
                        SortBy = sortBy
                    };
                }
                else
                {
                    // Đảm bảo ProviderId được set đúng
                    viewModel.ProviderId = providerId.Value;
                    Console.WriteLine($"[ServiceShopController] SUCCESS: ViewModel created");
                    Console.WriteLine($"[ServiceShopController]   ShopInfo.Name: '{viewModel.ShopInfo.Name}'");
                    Console.WriteLine($"[ServiceShopController]   ShopInfo.TotalServices: {viewModel.ShopInfo.TotalServices}");
                    Console.WriteLine($"[ServiceShopController]   ShopInfo.Rating: {viewModel.ShopInfo.Rating}");
                    Console.WriteLine($"[ServiceShopController]   ShopInfo.Logo: '{viewModel.ShopInfo.Logo}'");
                    Console.WriteLine($"[ServiceShopController]   Services count: {viewModel.Services.Count}");
                    Console.WriteLine($"[ServiceShopController]   BestsellingServices count: {viewModel.BestsellingServices.Count}");
                    System.Diagnostics.Debug.WriteLine($"SUCCESS: ViewModel created");
                    System.Diagnostics.Debug.WriteLine($"  ShopInfo.Name: '{viewModel.ShopInfo.Name}'");
                    System.Diagnostics.Debug.WriteLine($"  ShopInfo.TotalServices: {viewModel.ShopInfo.TotalServices}");
                    System.Diagnostics.Debug.WriteLine($"  Services count: {viewModel.Services.Count}");
                    System.Diagnostics.Debug.WriteLine($"  BestsellingServices count: {viewModel.BestsellingServices.Count}");
                }

                System.Diagnostics.Debug.WriteLine($"=== ServiceShopController.Index END ===");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPTION in ServiceShopController.Index: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Trả về view với ViewModel fallback thay vì error page
                var fallbackViewModel = new Models.ServiceShop.ServiceShopViewModel
                {
                    ProviderId = providerId.Value,
                    ShopInfo = new Models.ServiceShop.ShopInfo
                    {
                        Id = providerId.Value.GetHashCode(),
                        Name = "Đối tác VHS",
                        Logo = "/images/vhs_logo.png",
                        Status = "Online",
                        LastOnline = "Gần đây",
                        TotalServices = 0,
                        ResponseRate = 100.0
                    },
                    Services = new List<ServiceItem>(),
                    BestsellingServices = new List<ServiceItem>(),
                    ShopCategories = new List<Models.ServiceShop.ServiceCategory>(),
                    AllCategories = new List<Models.ServiceShop.ServiceCategory>(),
                    CurrentPage = 1,
                    TotalPages = 1,
                    SelectedCategoryId = categoryId,
                    SelectedTagId = tagId,
                    SortBy = sortBy
                };

                return View(fallbackViewModel);
            }
        }

        /// <summary>
        /// Chi tiết service (tạm thời không sử dụng)
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var service = await _serviceShopService.GetServiceByIdAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        /// <summary>
        /// API endpoint: Lấy bestselling services của provider
        /// </summary>
        public async Task<IActionResult> GetBestsellingServices(Guid providerId)
        {
            if (providerId == Guid.Empty)
            {
                return BadRequest("Provider ID is required");
            }

            try
            {
                var services = await _serviceShopService.GetBestsellingServicesAsync(providerId);
                return Json(services);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in GetBestsellingServices: {ex.Message}");
                return Json(new List<ServiceItem>());
            }
        }

        /// <summary>
        /// API endpoint: Lấy services theo category
        /// </summary>
        public async Task<IActionResult> GetServicesByCategory(Guid providerId, int categoryId, string sortBy = "popular", int page = 1)
        {
            if (providerId == Guid.Empty)
            {
                return BadRequest("Provider ID is required");
            }

            try
            {
                var services = await _serviceShopService.GetServicesByCategoryAsync(providerId, categoryId, sortBy, page);
                return Json(services);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in GetServicesByCategory: {ex.Message}");
                return Json(new List<ServiceItem>());
            }
        }
    }
}
