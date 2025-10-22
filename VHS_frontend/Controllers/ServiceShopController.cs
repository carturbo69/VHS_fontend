using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models;
using VHS_frontend.Services;

namespace VHS_frontend.Controllers
{
    public class ServiceShopController : Controller
    {
        private readonly ServiceShopService _serviceShopService;

        public ServiceShopController(ServiceShopService serviceShopService)
        {
            _serviceShopService = serviceShopService;
        }

        public async Task<IActionResult> Index(int? categoryId, string sortBy = "popular", int page = 1)
        {
            var viewModel = await _serviceShopService.GetServiceShopViewModelAsync(categoryId, sortBy, page);
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var service = await _serviceShopService.GetServiceByIdAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        public async Task<IActionResult> GetBestsellingServices()
        {
            var services = await _serviceShopService.GetBestsellingServicesAsync();
            return Json(services);
        }

        public async Task<IActionResult> GetServicesByCategory(int categoryId, string sortBy = "popular", int page = 1)
        {
            var services = await _serviceShopService.GetServicesByCategoryAsync(categoryId, sortBy, page);
            return Json(services);
        }
    }
}
