using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ServiceController : Controller
    {
        private readonly ProviderService _providerService;

        public ServiceController(ProviderService providerService)
        {
            _providerService = providerService;
        }

        // 🟢 Danh sách dịch vụ
        public async Task<IActionResult> Index()
        {
            var providerIdStr = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerIdStr))
            {
                TempData["Error"] = "Không tìm thấy ProviderID trong session!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var providerId = Guid.Parse(providerIdStr);
            var services = await _providerService.GetAllByProviderAsync(providerId);
            ViewBag.ApiRoot = "http://localhost:5154";
            return View(services);
        }

        // 🟣 Form thêm mới
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.ApiRoot = "http://localhost:5154";
            return View();
        }

        // 🟣 Xử lý thêm mới
        [HttpPost]
        public async Task<IActionResult> Create(ServiceViewModel model, IFormFile? Images)
        {
            var providerIdStr = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerIdStr))
            {
                TempData["Error"] = "Không tìm thấy ProviderID trong session!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            model.ProviderId = Guid.Parse(providerIdStr);

            var success = await _providerService.CreateAsync(model, Images);
            TempData[success ? "Success" : "Error"] = success ? "✅ Thêm dịch vụ thành công!" : "❌ Thêm thất bại!";
            return RedirectToAction("Index");
        }

        // 🟠 Form sửa
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var providerId = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerId))
            {
                TempData["Error"] = "Không tìm thấy ProviderID trong session!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var providerGuid = Guid.Parse(providerId);
            var services = await _providerService.GetAllByProviderAsync(providerGuid);
            var model = services.FirstOrDefault(s => s.ServiceId == id);

            if (model == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction("Index");
            }

            ViewBag.ApiRoot = "http://localhost:5154";
            return View(model);
        }

        // 🟠 Xử lý cập nhật
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, ServiceViewModel model, IFormFile? Images)
        {
            var success = await _providerService.UpdateAsync(id, model, Images);
            TempData[success ? "Success" : "Error"] = success ? "✅ Cập nhật thành công!" : "❌ Cập nhật thất bại!";
            return RedirectToAction("Index");
        }

        // 🔴 Xóa
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _providerService.DeleteAsync(id);
            TempData[success ? "Success" : "Error"] = success ? "🗑️ Xóa thành công!" : "❌ Xóa thất bại!";
            return RedirectToAction("Index");
        }
    }
}
