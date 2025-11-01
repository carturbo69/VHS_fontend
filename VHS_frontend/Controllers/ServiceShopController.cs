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

        public async Task<IActionResult> Index(Guid? providerId, int? categoryId, Guid? tagId, string sortBy = "popular", int page = 1)
        {
            if (!providerId.HasValue)
            {
                return BadRequest("Provider ID is required");
            }

            var viewModel = await _serviceShopService.GetServiceShopViewModelAsync(providerId.Value, categoryId, tagId, sortBy, page);
            if (viewModel == null)
            {
                return NotFound("Provider shop not found");
            }
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

        public async Task<IActionResult> GetBestsellingServices(Guid providerId)
        {
            var services = await _serviceShopService.GetBestsellingServicesAsync(providerId);
            return Json(services);
        }

        public async Task<IActionResult> GetServicesByCategory(Guid providerId, int categoryId, string sortBy = "popular", int page = 1)
        {
            var services = await _serviceShopService.GetServicesByCategoryAsync(providerId, categoryId, sortBy, page);
            return Json(services);
        }
    }
}
