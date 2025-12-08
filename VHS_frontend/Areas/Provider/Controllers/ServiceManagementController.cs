using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models.Service;
using VHS_frontend.Areas.Provider.Models.Tag;
using VHS_frontend.Areas.Provider.Models.Option;
using System.Text.RegularExpressions;
using System.Linq;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ServiceManagementController : Controller
    {
        private readonly ServiceManagementService _serviceManagementService;
        private readonly ProviderProfileService _providerProfileService;
        private readonly IConfiguration _configuration;

        public ServiceManagementController(
            ServiceManagementService serviceManagementService,
            ProviderProfileService providerProfileService,
            IConfiguration configuration)
        {
            _serviceManagementService = serviceManagementService;
            _providerProfileService = providerProfileService;
            _configuration = configuration;
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

        // POST: Provider/ServiceManagement/ToggleStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id, string targetStatus)
        {
            var token = HttpContext.Session.GetString("JWToken");

            try
            {
                var resp = await _serviceManagementService.UpdateStatusAsync(id, targetStatus, token);
                if (resp?.Success == true)
                {
                    TempData["Success"] = targetStatus == "Active" ? "Đã bật hoạt động dịch vụ." : "Đã tạm dừng dịch vụ.";
                }
                else
                {
                    TempData["Error"] = resp?.Message ?? "Không thể cập nhật trạng thái dịch vụ.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
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
            ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";
            return View(services ?? new List<ServiceProviderReadDTO>());
        }

        // GET: Provider/ServiceManagement/Create
        // Lấy categories chỉ từ certificates còn tồn tại của provider này
        public async Task<IActionResult> Create()
        {
            var token = HttpContext.Session.GetString("JWToken");
            var providerId = await GetProviderIdFromSession(token);

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin nhà cung cấp. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Lấy danh sách Categories khả dụng - chỉ lấy từ certificates còn tồn tại của provider này
            // Backend API sẽ lọc categories từ certificates, nên khi admin xóa certificate thì category sẽ không còn
            var categories = await _serviceManagementService.GetAvailableCategoriesAsync(providerId, token);
            ViewBag.Categories = categories ?? new List<CategoryDTO>();

            // Không nạp Options dùng chung cho trang tạo mới (bắt đầu trống, provider tự thêm)

            ViewData["Title"] = "Tạo Dịch vụ mới";
            ViewBag.ProviderId = providerId;
            ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";

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

            // Bind OptionValues từ form data (Dictionary binding từ multipart form)
            if (model.OptionValues == null)
            {
                model.OptionValues = new Dictionary<Guid, string>();
            }
            
            // Thu thập OptionValues từ form data (format: OptionValues[key] = value)
            var form = await Request.ReadFormAsync();
            foreach (var key in form.Keys.Where(k => k != null && k.StartsWith("OptionValues[")))
            {
                // Parse key từ format "OptionValues[guid]" 
                var match = Regex.Match(key, @"OptionValues\[([^\]]+)\]");
                if (match.Success && Guid.TryParse(match.Groups[1].Value, out var optionId))
                {
                    var value = form[key].ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        model.OptionValues[optionId] = value;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Reload dropdown data
                var categories = await _serviceManagementService.GetAvailableCategoriesAsync(providerId, token);
                ViewBag.Categories = categories ?? new List<CategoryDTO>();
                // Không nạp Options ở trang tạo mới
                ViewBag.ProviderId = providerId;
                ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";

                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            // Gộp input hình ảnh: nếu người dùng chỉ upload qua Images (multi), lấy ảnh đầu tiên làm Avatar
            if ((model.Avatar == null || model.Avatar.Length == 0) && model.Images != null && model.Images.Count > 0)
            {
                model.Avatar = model.Images.FirstOrDefault();
                model.Images = model.Images.Skip(1).ToList();
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
                ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";

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
            
            // Lưu status hiện tại vào ViewBag để kiểm tra trong view
            ViewBag.CurrentStatus = service.Status;

            // Tạo dictionary OptionValues từ Options (nếu có Value)
            // Dùng Dictionary<string, string> để đảm bảo keys là string khi serialize sang JSON
            var optionValues = new Dictionary<string, string>();
            if (service.Options != null)
            {
                foreach (var opt in service.Options.Where(opt => !string.IsNullOrWhiteSpace(opt.Value)))
                {
                    optionValues[opt.OptionId.ToString()] = opt.Value ?? string.Empty;
                }
            }
            ViewBag.OptionValues = optionValues;

            // Load tags for the service's category
            var tags = await _serviceManagementService.GetTagsByCategoryAsync(service.CategoryId.ToString(), providerId, token);
            ViewBag.Tags = tags ?? new List<TagDTO>();

        // Only pass options already selected with this service
        ViewBag.Options = service.Options ?? new List<OptionDTO>();

            ViewData["Title"] = "Chỉnh sửa Dịch vụ";
            ViewBag.ServiceId = id;
            ViewBag.CurrentImageUrl = service.Images;
            ViewBag.CategoryName = service.CategoryName;
            ViewBag.CategoryId = service.CategoryId.ToString();
            ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";

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
                    var providerId = await GetProviderIdFromSession(token);
                    var tags = await _serviceManagementService.GetTagsByCategoryAsync(service.CategoryId.ToString(), providerId, token);
                    ViewBag.Tags = tags ?? new List<TagDTO>();
                    ViewBag.Options = service.Options ?? new List<OptionDTO>();
                    ViewBag.CurrentImageUrl = service.Images;
                    ViewBag.CategoryName = service.CategoryName;
                    ViewBag.CategoryId = service.CategoryId.ToString();
                    
                    // Tạo dictionary OptionValues từ Options (nếu có Value)
                    var optionValues = new Dictionary<string, string>();
                    if (service.Options != null)
                    {
                        foreach (var opt in service.Options.Where(opt => !string.IsNullOrWhiteSpace(opt.Value)))
                        {
                            optionValues[opt.OptionId.ToString()] = opt.Value ?? string.Empty;
                        }
                    }
                    ViewBag.OptionValues = optionValues;
                }
                ViewBag.ServiceId = id;
                ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";

                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            // Không tự động nâng ảnh đầu tiên thành Avatar trong trang chỉnh sửa
            // Ảnh mới sẽ được thêm vào cuối danh sách; Avatar chỉ thay khi người dùng chọn Avatar riêng

            // Lấy service hiện tại để kiểm tra status
            var currentService = await _serviceManagementService.GetServiceByIdAsync(id, token);
            if (currentService == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction("Index");
            }
            
            // Xử lý status dựa trên trạng thái hiện tại
            if (string.Equals(currentService.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentService.Status, "PendingUpdate", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu đang chờ duyệt, giữ nguyên status
                model.Status = currentService.Status;
            }
            else if (string.Equals(currentService.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu đang tạm dừng, chuyển thành "Pending" khi sửa để tránh lách luật
                model.Status = "Pending";
            }
            // Nếu là "Active", giữ nguyên status từ model (cho phép provider chọn Active/Inactive)
            
            // Bind OptionValues từ form data (Dictionary binding từ multipart form)
            if (model.OptionValues == null)
            {
                model.OptionValues = new Dictionary<Guid, string>();
            }
            
            // Thu thập OptionValues từ form data (format: OptionValues[key] = value)
            var form = await Request.ReadFormAsync();
            foreach (var key in form.Keys.Where(k => k != null && k.StartsWith("OptionValues[")))
            {
                // Parse key từ format "OptionValues[guid]" 
                var match = Regex.Match(key, @"OptionValues\[([^\]]+)\]");
                if (match.Success && Guid.TryParse(match.Groups[1].Value, out var optionId))
                {
                    var value = form[key].ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        model.OptionValues[optionId] = value;
                    }
                }
            }
            
            // Thu thập RemoveImages từ form data (nhiều input hidden với name="RemoveImages")
            if (model.RemoveImages == null)
            {
                model.RemoveImages = new List<string>();
            }
            // Lấy tất cả giá trị RemoveImages từ form (có thể có nhiều input với cùng name)
            if (form.ContainsKey("RemoveImages"))
            {
                var removeImages = form["RemoveImages"];
                foreach (var removePath in removeImages)
                {
                    if (!string.IsNullOrWhiteSpace(removePath))
                    {
                        // Normalize path: remove leading/trailing whitespace và đảm bảo format đúng
                        var normalizedPath = removePath.Trim();
                        // Nếu path bắt đầu bằng http, cần extract relative path
                        if (normalizedPath.StartsWith("http://") || normalizedPath.StartsWith("https://"))
                        {
                            try
                            {
                                // Extract path sau domain (ví dụ: http://localhost:5154/uploads/... -> /uploads/...)
                                var uri = new Uri(normalizedPath);
                                normalizedPath = uri.AbsolutePath;
                            }
                            catch
                            {
                                // Nếu không parse được URI, giữ nguyên path
                            }
                        }
                        // Đảm bảo path bắt đầu bằng /
                        if (!normalizedPath.StartsWith("/"))
                        {
                            normalizedPath = "/" + normalizedPath;
                        }
                        model.RemoveImages.Add(normalizedPath);
                    }
                }
            }
            
            // Debug log
            if (model.RemoveImages != null && model.RemoveImages.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveImages count: {model.RemoveImages.Count}");
                foreach (var path in model.RemoveImages)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {path}");
                }
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
                    var providerId = await GetProviderIdFromSession(token);
                    var tags = await _serviceManagementService.GetTagsByCategoryAsync(service.CategoryId.ToString(), providerId, token);
                    ViewBag.Tags = tags ?? new List<TagDTO>();
                    ViewBag.Options = service.Options ?? new List<OptionDTO>();
                    ViewBag.CurrentImageUrl = service.Images;
                    ViewBag.CategoryName = service.CategoryName;
                    ViewBag.CategoryId = service.CategoryId.ToString();
                    
                    // Tạo dictionary OptionValues từ Options (nếu có Value)
                    var optionValues = new Dictionary<string, string>();
                    if (service.Options != null)
                    {
                        foreach (var opt in service.Options.Where(opt => !string.IsNullOrWhiteSpace(opt.Value)))
                        {
                            optionValues[opt.OptionId.ToString()] = opt.Value ?? string.Empty;
                        }
                    }
                    ViewBag.OptionValues = optionValues;
                }
                ViewBag.ServiceId = id;
                ViewBag.BackendBase = _configuration["Apis:Backend"] ?? "http://localhost:5154";

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
        // Lọc tags theo providerId từ session để chỉ hiển thị tags còn lại trong certificates
        [HttpGet]
        public async Task<IActionResult> GetTagsByCategory(string categoryId)
        {
            var token = HttpContext.Session.GetString("JWToken");
            var providerId = await GetProviderIdFromSession(token);
            
            if (string.IsNullOrEmpty(providerId))
            {
                return Json(new List<TagDTO>()); // Trả về rỗng nếu không có providerId
            }
            
            // Truyền providerId vào để backend lọc tags từ certificates còn tồn tại
            var tags = await _serviceManagementService.GetTagsByCategoryAsync(categoryId, providerId, token);
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

