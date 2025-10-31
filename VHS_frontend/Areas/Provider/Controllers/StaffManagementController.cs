using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using VHS_frontend.Areas.Provider.Models.Staff;
using VHS_frontend.Services.Provider;
using System.Net.Http;
using System.Text.Json;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class StaffManagementController : Controller
    {
        private readonly StaffManagementService _staffManagementService;

        public StaffManagementController(StaffManagementService staffManagementService)
        {
            _staffManagementService = staffManagementService;
        }

        // GET: Provider/StaffManagement
        public async Task<IActionResult> Index()
        {
            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                var token = HttpContext.Session.GetString("JWToken");
                
                if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Lấy ProviderId từ API
                string providerId = await GetProviderIdFromAccountId(accountId, token);
                
                if (string.IsNullOrEmpty(providerId))
                {
                    TempData["Error"] = "Không thể lấy thông tin Provider. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Get staff list from service
                var staffList = await _staffManagementService.GetStaffByProviderAsync(providerId, token);
                
                if (staffList == null)
                {
                    staffList = new List<StaffDTO>();
                }
                
                // Debug log dữ liệu từ Backend
                Console.WriteLine($"📊 Staff data from Backend:");
                Console.WriteLine($"Total staff: {staffList.Count}");
                foreach (var staff in staffList)
                {
                    Console.WriteLine($"- {staff.StaffName}: IsLocked = {staff.IsLocked}");
                }
                
                // Sắp xếp danh sách: IsDeleted = false trước, IsDeleted = true sau
                var sortedStaffList = staffList
                    .OrderBy(s => s.IsLocked)  // false (hoạt động) trước, true (bị khóa) sau
                    .ThenBy(s => s.StaffName)  // Sắp xếp theo tên trong cùng trạng thái
                    .ToList();
                
                Console.WriteLine($"📊 Sorted staff list:");
                foreach (var staff in sortedStaffList)
                {
                    Console.WriteLine($"- {staff.StaffName}: IsLocked = {staff.IsLocked}");
                }
                
                return View(sortedStaffList);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải danh sách nhân viên: " + ex.Message;
                return View(new List<StaffDTO>());
            }
        }

        // GET: Provider/StaffManagement/Create
        [HttpGet]
        [Route("Provider/StaffManagement/Create")]
        public IActionResult Create()
        {
            try
            {
                Console.WriteLine("[DEBUG] ===== Create GET Action Called =====");
                var model = new StaffCreateDTO();
                Console.WriteLine("[DEBUG] Model created successfully");
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error in Create GET: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra khi tải trang: " + ex.Message;
                return View(new StaffCreateDTO());
            }
        }

        // POST: Provider/StaffManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                var token = HttpContext.Session.GetString("JWToken");
                
                if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Session hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Create");
                }

                // Get provider ID
                string providerId = await GetProviderIdFromAccountId(accountId, token);
                if (string.IsNullOrEmpty(providerId))
                {
                    TempData["Error"] = "Không thể lấy thông tin Provider.";
                    return RedirectToAction("Create");
                }

                // Create MultipartFormDataContent for Backend API
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(model.StaffName), "StaffName");
                formData.Add(new StringContent(model.Password), "Password");
                formData.Add(new StringContent(model.CitizenID), "CitizenID");
                
                if (model.FaceImage != null)
                {
                    formData.Add(new StreamContent(model.FaceImage.OpenReadStream()), "FaceImage", model.FaceImage.FileName);
                }
                if (model.CitizenIDFrontImage != null)
                {
                    formData.Add(new StreamContent(model.CitizenIDFrontImage.OpenReadStream()), "CitizenIDFrontImage", model.CitizenIDFrontImage.FileName);
                }
                if (model.CitizenIDBackImage != null)
                {
                    formData.Add(new StreamContent(model.CitizenIDBackImage.OpenReadStream()), "CitizenIDBackImage", model.CitizenIDBackImage.FileName);
                }

                // Call Backend API
                var result = await _staffManagementService.CreateStaffAsync(providerId, formData, token);
                
                if (result.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Tạo nhân viên thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    TempData["Error"] = "Lỗi: " + errorContent;
                    return RedirectToAction("Create");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Create");
            }
        }

        // GET: Provider/StaffManagement/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var staff = await _staffManagementService.GetStaffByIdAsync(id, token);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên";
                    return RedirectToAction("Index");
                }

                // Process current images to ensure proper URLs
                var currentFaceImage = !string.IsNullOrEmpty(staff.FaceImage) 
                    ? (staff.FaceImage.StartsWith("http") ? staff.FaceImage : $"http://localhost:5154{staff.FaceImage}")
                    : null;

                var editModel = new StaffUpdateDTO
                {
                    StaffName = staff.StaffName,
                    FaceImage = null, // New file will be uploaded
                    CitizenID = staff.CitizenID,
                    CitizenIDFrontImage = null, // New file will be uploaded
                    CitizenIDBackImage = null, // New file will be uploaded
                    CurrentFaceImage = currentFaceImage,
                    CurrentCitizenIDFrontImage = !string.IsNullOrEmpty(staff.CitizenIDFrontImage) 
                        ? (staff.CitizenIDFrontImage.StartsWith("http") ? staff.CitizenIDFrontImage : $"http://localhost:5154{staff.CitizenIDFrontImage}")
                        : null,
                    CurrentCitizenIDBackImage = !string.IsNullOrEmpty(staff.CitizenIDBackImage) 
                        ? (staff.CitizenIDBackImage.StartsWith("http") ? staff.CitizenIDBackImage : $"http://localhost:5154{staff.CitizenIDBackImage}")
                        : null
                };

                return View(editModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải thông tin nhân viên: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Provider/StaffManagement/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, StaffUpdateDTO model)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Validate images only if provided
                if (model.FaceImage != null)
                {
                    ValidateImageFile(model.FaceImage, "FaceImage", "Ảnh chân dung");
                }
                if (model.CitizenIDFrontImage != null)
                {
                    ValidateImageFile(model.CitizenIDFrontImage, "CitizenIDFrontImage", "Ảnh mặt trước CCCD");
                }
                if (model.CitizenIDBackImage != null)
                {
                    ValidateImageFile(model.CitizenIDBackImage, "CitizenIDBackImage", "Ảnh mặt sau CCCD");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Create FormData for multipart/form-data upload
                Console.WriteLine("[DEBUG] Creating FormData for multipart update...");
                
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(model.StaffName), "StaffName");
                formData.Add(new StringContent(model.CitizenID), "CitizenID");
                
                // Add image files directly to FormData if provided
                if (model.FaceImage != null)
                {
                    var faceImageContent = new StreamContent(model.FaceImage.OpenReadStream());
                    faceImageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.FaceImage.ContentType);
                    formData.Add(faceImageContent, "FaceImage", model.FaceImage.FileName);
                    Console.WriteLine($"[DEBUG] Added FaceImage: {model.FaceImage.FileName}");
                }
                
                if (model.CitizenIDFrontImage != null)
                {
                    var frontImageContent = new StreamContent(model.CitizenIDFrontImage.OpenReadStream());
                    frontImageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.CitizenIDFrontImage.ContentType);
                    formData.Add(frontImageContent, "CitizenIDFrontImage", model.CitizenIDFrontImage.FileName);
                    Console.WriteLine($"[DEBUG] Added CitizenIDFrontImage: {model.CitizenIDFrontImage.FileName}");
                }
                
                if (model.CitizenIDBackImage != null)
                {
                    var backImageContent = new StreamContent(model.CitizenIDBackImage.OpenReadStream());
                    backImageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.CitizenIDBackImage.ContentType);
                    formData.Add(backImageContent, "CitizenIDBackImage", model.CitizenIDBackImage.FileName);
                    Console.WriteLine($"[DEBUG] Added CitizenIDBackImage: {model.CitizenIDBackImage.FileName}");
                }
                
                Console.WriteLine($"[DEBUG] FormData contains {formData.Count()} parts");
                
                var result = await _staffManagementService.UpdateStaffAsync(id, formData, token);
                
                if (result.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật nhân viên thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Cập nhật nhân viên thất bại! Lỗi: {result.StatusCode} - {errorContent}";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }



        private void ValidateImageFile(IFormFile? file, string fieldName, string displayName)
        {
            if (file != null)
            {
                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(fieldName, $"{displayName} không được vượt quá 5MB");
                }
                
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    ModelState.AddModelError(fieldName, $"{displayName} chỉ chấp nhận file JPG, PNG, GIF");
                }
            }
        }


        private string? GetCurrentImagePath(string? currentImage)
        {
            if (string.IsNullOrEmpty(currentImage)) return null;
            
            // If already a full URL, return as is
            if (currentImage.StartsWith("http://") || currentImage.StartsWith("https://"))
            {
                return currentImage;
            }
            
            // If it's a relative path, convert to full URL
            return $"http://localhost:5154{currentImage}";
        }

        /// <summary>
        /// Lấy ProviderId từ AccountId thông qua API backend
        /// </summary>
        private async Task<string> GetProviderIdFromAccountId(string accountId, string token)
        {
            try
            {
                var response = await _staffManagementService.GetProviderIdFromAccountId(accountId, token);
                
                if (response.IsSuccessStatusCode)
                {
                    var providerId = await response.Content.ReadAsStringAsync();
                    return providerId.Trim('"'); // Remove quotes if any
                }
                
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentProviderId()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var token = HttpContext.Session.GetString("JWToken");
            
            // Debug session
            Console.WriteLine($"[DEBUG] GetCurrentProviderId - AccountID: {accountId}");
            Console.WriteLine($"[DEBUG] GetCurrentProviderId - Token: {token?.Substring(0, 10)}...");
            Console.WriteLine($"[DEBUG] GetCurrentProviderId - Session Keys: {string.Join(", ", HttpContext.Session.Keys)}");
            
            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(token))
            {
                return Json(new { error = "Session expired", accountId = accountId, token = token?.Substring(0, 10) });
            }
            
            try
            {
                var providerId = await GetProviderIdFromAccountId(accountId, token);
                Console.WriteLine($"[DEBUG] GetCurrentProviderId - ProviderID: {providerId}");
                return Json(new { providerId = providerId, token = token });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] GetCurrentProviderId - Error: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // 🔒 Khóa tài khoản Staff
        [HttpPost("{id}/lock")]
        [Route("Provider/StaffManagement/{id}/lock")]
        public async Task<IActionResult> LockStaff(string id)
        {
            try
            {
                Console.WriteLine($"🔒 LockStaff called with id: {id}");
                
                var token = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ No token found in session");
                    return Json(new { error = "Session hết hạn. Vui lòng đăng nhập lại." });
                }

                Console.WriteLine($"✅ Token found: {token.Substring(0, 20)}...");
                
                var result = await _staffManagementService.LockStaffAsync(id, token);
                Console.WriteLine($"📡 Backend response status: {result.StatusCode}");
                
                if (result.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Lock successful");
                    return Json(new { success = true, message = "Đã khóa tài khoản nhân viên thành công!" });
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Backend error: {errorContent}");
                    return Json(new { error = errorContent });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Exception in LockStaff: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // 🔓 Mở khóa tài khoản Staff
        [HttpPost("{id}/unlock")]
        [Route("Provider/StaffManagement/{id}/unlock")]
        public async Task<IActionResult> UnlockStaff(string id)
        {
            try
            {
                Console.WriteLine($"🔓 UnlockStaff called with id: {id}");
                
                var token = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ No token found in session");
                    return Json(new { error = "Session hết hạn. Vui lòng đăng nhập lại." });
                }

                Console.WriteLine($"✅ Token found: {token.Substring(0, 20)}...");
                
                var result = await _staffManagementService.UnlockStaffAsync(id, token);
                Console.WriteLine($"📡 Backend response status: {result.StatusCode}");
                
                if (result.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Unlock successful");
                    return Json(new { success = true, message = "Đã mở khóa tài khoản nhân viên thành công!" });
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Backend error: {errorContent}");
                    return Json(new { error = errorContent });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Exception in UnlockStaff: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // ✨ MỚI: Xem chi tiết nhân viên và lịch làm việc tuần
        [HttpGet]
        [Route("Provider/StaffManagement/Details/{id}")]
        public async Task<IActionResult> Details(string id, [FromQuery] DateTime? weekStart)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Lấy thông tin staff
                var staff = await _staffManagementService.GetStaffByIdAsync(id, token);
                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy nhân viên";
                    return RedirectToAction("Index");
                }

                // Lấy lịch tuần (mặc định tuần này nếu không truyền weekStart)
                var start = weekStart ?? DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
                
                // Call API để lấy schedule
                var scheduleResponse = await _staffManagementService.GetWeeklyScheduleAsync(id, start, token);
                
                StaffScheduleResponse? scheduleData = null;
                if (scheduleResponse.IsSuccessStatusCode)
                {
                    var content = await scheduleResponse.Content.ReadAsStringAsync();
                    scheduleData = JsonSerializer.Deserialize<StaffScheduleResponse>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }

                ViewBag.Staff = staff;
                ViewBag.WeekStart = start;
                ViewBag.WeekEnd = start.AddDays(7);
                ViewBag.Schedule = scheduleData?.Schedule ?? new List<StaffScheduleDTO>();
                
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Details: {ex.Message}");
                TempData["Error"] = "Không thể tải thông tin nhân viên: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // 🔑 Cập nhật mật khẩu cho Staff
        [HttpPost("{id}/update-password")]
        [Route("Provider/StaffManagement/{id}/update-password")]
        public async Task<IActionResult> UpdatePassword(string id, [FromBody] StaffUpdatePasswordDTO model)
        {
            try
            {
                Console.WriteLine($"🔑 UpdatePassword called with id: {id}");
                
                var token = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ No token found in session");
                    return Json(new { error = "Session hết hạn. Vui lòng đăng nhập lại." });
                }

                Console.WriteLine($"✅ Token found: {token.Substring(0, 20)}...");
                
                var result = await _staffManagementService.UpdateStaffPasswordAsync(id, model, token);
                Console.WriteLine($"📡 Backend response status: {result.StatusCode}");
                
                if (result.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Password update successful");
                    return Json(new { success = true, message = "Cập nhật mật khẩu thành công!" });
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Backend error: {errorContent}");
                    return Json(new { error = errorContent });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Exception in UpdatePassword: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }
    }
}
