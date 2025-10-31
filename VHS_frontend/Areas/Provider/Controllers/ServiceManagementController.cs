using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models.Service;
using VHS_frontend.Areas.Provider.Models.Tag;
using VHS_frontend.Areas.Provider.Models.Option;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ServiceManagementController : Controller
    {
        private readonly ServiceManagementService _serviceManagementService;
        private readonly ProviderProfileService _providerProfileService;

        public ServiceManagementController(
            ServiceManagementService serviceManagementService,
            ProviderProfileService providerProfileService)
        {
            _serviceManagementService = serviceManagementService;
            _providerProfileService = providerProfileService;
        }

        private async Task<string?> GetProviderIdFromSession(string? token)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            if (string.IsNullOrEmpty(accountId)) return null;

            var providerId = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerId))
            {
                providerId = await _providerProfileService.GetProviderIdByAccountAsync(accountId, token);
                if (!string.IsNullOrEmpty(providerId))
                {
                    HttpContext.Session.SetString("ProviderID", providerId);
                }
            }
            return providerId;
        }

        // GET: Provider/ServiceManagement
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWToken");
            var providerId = await GetProviderIdFromSession(token);

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin nhà cung cấp. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var services = await _serviceManagementService.GetServicesByProviderAsync(providerId, token);

            ViewData["Title"] = "Quản lý Dịch vụ";
            return View(services ?? new List<ServiceProviderReadDTO>());
        }

        // GET: Provider/ServiceManagement/Create
        public async Task<IActionResult> Create()
        {
            var token = HttpContext.Session.GetString("JWToken");
            var providerId = await GetProviderIdFromSession(token);

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin nhà cung cấp. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Lấy danh sách Categories khả dụng
            var categories = await _serviceManagementService.GetAvailableCategoriesAsync(providerId, token);
            ViewBag.Categories = categories ?? new List<CategoryDTO>();

            // Không nạp Options dùng chung cho trang tạo mới (bắt đầu trống, provider tự thêm)

            ViewData["Title"] = "Tạo Dịch vụ mới";
            ViewBag.ProviderId = providerId;

            return View(new ServiceProviderCreateDTO { ProviderId = Guid.Parse(providerId) });
        }

        // POST: Provider/ServiceManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceProviderCreateDTO model)
        {
            var token = HttpContext.Session.GetString("JWToken");
            var providerId = await GetProviderIdFromSession(token);

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin nhà cung cấp. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            model.ProviderId = Guid.Parse(providerId);

            if (!ModelState.IsValid)
            {
                // Reload dropdown data
                var categories = await _serviceManagementService.GetAvailableCategoriesAsync(providerId, token);
                ViewBag.Categories = categories ?? new List<CategoryDTO>();
                // Không nạp Options ở trang tạo mới
                ViewBag.ProviderId = providerId;

                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            var response = await _serviceManagementService.CreateServiceAsync(model, token);

            if (response?.Success == true)
            {
                TempData["Success"] = "Tạo dịch vụ thành công!";
                return RedirectToAction("Index");
            }
            else
            {
                // Reload dropdown data
                var categories = await _serviceManagementService.GetAvailableCategoriesAsync(providerId, token);
                ViewBag.Categories = categories ?? new List<CategoryDTO>();
                // Không nạp Options ở trang tạo mới
                ViewBag.ProviderId = providerId;

                TempData["Error"] = response?.Message ?? "Tạo dịch vụ thất bại.";
                return View(model);
            }
        }

        // GET: Provider/ServiceManagement/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            var providerId = await GetProviderIdFromSession(token);

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin nhà cung cấp. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var service = await _serviceManagementService.GetServiceByIdAsync(id, token);
            if (service == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction("Index");
            }

            // Map to UpdateDTO
            var updateModel = new ServiceProviderUpdateDTO
            {
                Title = service.Title,
                Description = service.Description,
                Price = service.Price,
                UnitType = service.UnitType,
                BaseUnit = service.BaseUnit,
                Status = service.Status,
                TagIds = service.Tags.Select(t => t.TagId).ToList(),
                OptionIds = service.Options.Select(o => o.OptionId).ToList()
            };

            // Load tags for the service's category
            var tags = await _serviceManagementService.GetTagsByCategoryAsync(service.CategoryId.ToString(), token);
            ViewBag.Tags = tags ?? new List<TagDTO>();

            // Only pass options already selected with this service
            ViewBag.Options = service.Options ?? new List<OptionDTO>();

            ViewData["Title"] = "Chỉnh sửa Dịch vụ";
            ViewBag.ServiceId = id;
            ViewBag.CurrentImageUrl = service.Images;
            ViewBag.CategoryName = service.CategoryName;

            return View(updateModel);
        }

        // POST: Provider/ServiceManagement/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ServiceProviderUpdateDTO model)
        {
            var token = HttpContext.Session.GetString("JWToken");

            if (!ModelState.IsValid)
            {
                // Reload data
                var service = await _serviceManagementService.GetServiceByIdAsync(id, token);
                if (service != null)
                {
                    var tags = await _serviceManagementService.GetTagsByCategoryAsync(service.CategoryId.ToString(), token);
                    ViewBag.Tags = tags ?? new List<TagDTO>();
                    ViewBag.Options = service.Options ?? new List<OptionDTO>();
                    ViewBag.CurrentImageUrl = service.Images;
                    ViewBag.CategoryName = service.CategoryName;
                }
                ViewBag.ServiceId = id;

                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            var response = await _serviceManagementService.UpdateServiceAsync(id, model, token);

            if (response?.Success == true)
            {
                TempData["Success"] = "Cập nhật dịch vụ thành công!";
                return RedirectToAction("Index");
            }
            else
            {
                // Reload data
                var service = await _serviceManagementService.GetServiceByIdAsync(id, token);
                if (service != null)
                {
                    var tags = await _serviceManagementService.GetTagsByCategoryAsync(service.CategoryId.ToString(), token);
                    ViewBag.Tags = tags ?? new List<TagDTO>();
                    ViewBag.Options = service.Options ?? new List<OptionDTO>();
                    ViewBag.CurrentImageUrl = service.Images;
                    ViewBag.CategoryName = service.CategoryName;
                }
                ViewBag.ServiceId = id;

                TempData["Error"] = response?.Message ?? "Cập nhật dịch vụ thất bại.";
                return View(model);
            }
        }

        // POST: Provider/ServiceManagement/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");

            var response = await _serviceManagementService.DeleteServiceAsync(id, token);

            if (response?.Success == true)
            {
                TempData["Success"] = "Xóa dịch vụ thành công!";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Xóa dịch vụ thất bại.";
            }

            return RedirectToAction("Index");
        }

        // API endpoint to get tags by category (for AJAX)
        [HttpGet]
        public async Task<IActionResult> GetTagsByCategory(string categoryId)
        {
            var token = HttpContext.Session.GetString("JWToken");
            var tags = await _serviceManagementService.GetTagsByCategoryAsync(categoryId, token);
            return Json(tags ?? new List<TagDTO>());
        }

        // API endpoint to get all options (for AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAllOptions()
        {
            var token = HttpContext.Session.GetString("JWToken");
            var options = await _serviceManagementService.GetAllOptionsAsync(token);
            return Json(options ?? new List<OptionDTO>());
        }

        // API endpoint to create new tag (disabled)
        [HttpPost]
        public IActionResult CreateTag([FromBody] VHS_frontend.Areas.Provider.Models.Tag.TagCreateDTO model)
        {
            return Json(new { success = false, message = "Tính năng tạo Tag đã bị vô hiệu hoá đối với Provider." });
        }

        // API endpoint to create new option
        [HttpPost]
        public async Task<IActionResult> CreateOption([FromBody] VHS_frontend.Areas.Provider.Models.Option.OptionCreateDTO model)
        {
            var token = HttpContext.Session.GetString("JWToken");
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                var errorMessage = string.Join("; ", errors);
                return Json(new { success = false, message = $"Dữ liệu không hợp lệ: {errorMessage}" });
            }

            try
            {
                var response = await _serviceManagementService.CreateOptionAsync(model, token);
                return Json(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateOption Exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API endpoint to delete tag (disabled for Provider)
        [HttpDelete]
        public IActionResult DeleteTag(string id)
        {
            return Json(new { success = false, message = "Provider không được phép xoá Tag." });
        }

        // API endpoint to delete option
        [HttpDelete]
        public async Task<IActionResult> DeleteOption(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");

            try
            {
                var response = await _serviceManagementService.DeleteOptionAsync(id, token);
                return Json(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteOption Exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

