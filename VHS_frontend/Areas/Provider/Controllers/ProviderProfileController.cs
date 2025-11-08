using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models.Profile;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderProfileController : Controller
    {
        private readonly ProviderProfileService _providerProfileService;

        public ProviderProfileController(ProviderProfileService providerProfileService)
        {
            _providerProfileService = providerProfileService;
        }

        // GET: Provider/ProviderProfile/Test
        public IActionResult Test()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            
            return Json(new { 
                AccountID = accountId, 
                Username = username, 
                Role = role,
                Message = "Session test successful"
            });
        }

        // GET: Provider/ProviderProfile
        public async Task<IActionResult> Index()
        {
            try
            {
                // Debug: Kiểm tra session
                var accountId = HttpContext.Session.GetString("AccountID");
                var username = HttpContext.Session.GetString("Username");
                var role = HttpContext.Session.GetString("Role");
                
                Console.WriteLine($"[DEBUG] Session - AccountID: {accountId}, Username: {username}, Role: {role}");
                
                if (string.IsNullOrEmpty(accountId))
                {
                    Console.WriteLine("[DEBUG] AccountID is null or empty, redirecting to login");
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var token = HttpContext.Session.GetString("JWToken");
                Console.WriteLine($"[DEBUG] Calling GetProfileAsync with AccountID: {accountId}, Token: {token?.Substring(0, 10)}...");
                var profile = await _providerProfileService.GetProfileAsync(accountId, token);
                Console.WriteLine($"[DEBUG] Profile result: {profile?.ProviderName ?? "null"}");
                
                if (profile == null)
                {
                    // Nếu API trả về null, tạo profile mặc định
                    profile = new ProviderProfileDTO
                    {
                        ProviderId = accountId,
                        AccountId = accountId,
                        AccountName = username ?? "Provider",
                        Email = "provider@example.com",
                        ProviderName = "Nhà cung cấp mới",
                        PhoneNumber = "",
                        Description = "",
                        Images = "",
                        Status = "Pending"
                    };
                    Console.WriteLine($"[DEBUG] Created default profile for new provider");
                }
                
                return View(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Exception in Index: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Không thể tải thông tin profile: " + ex.Message;
                return View(new ProviderProfileDTO());
            }
        }

        // GET: Provider/ProviderProfile/Edit
        public async Task<IActionResult> Edit()
        {
            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Lấy dữ liệu profile hiện tại
                var token = HttpContext.Session.GetString("JWToken");
                var profile = await _providerProfileService.GetProfileAsync(accountId, token);
                var updateModel = new ProviderProfileUpdateDTO
                {
                    ProviderName = profile?.ProviderName ?? "",
                    PhoneNumber = profile?.PhoneNumber ?? "",
                    Description = profile?.Description ?? "",
                    Images = profile?.Images ?? ""
                };

                return View(updateModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải thông tin profile: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Provider/ProviderProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProviderProfileUpdateDTO model)
        {
            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Xử lý file upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Kiểm tra kích thước file (5MB)
                    if (model.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "File hình ảnh không được vượt quá 5MB");
                        return View(model);
                    }

                    // Kiểm tra loại file
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(model.ImageFile.ContentType.ToLower()))
                    {
                        ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file JPG, PNG, GIF");
                        return View(model);
                    }

                    // Tạo tên file unique
                    var fileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "provider-images");
                    
                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    // Lưu file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    // Cập nhật URL hình ảnh
                    model.Images = $"/uploads/provider-images/{fileName}";
                    Console.WriteLine($"[DEBUG] File uploaded: {model.Images}");
                }
                else
                {
                    // Nếu không có file mới, giữ nguyên URL cũ
                    Console.WriteLine($"[DEBUG] No new file uploaded, keeping existing: {model.Images}");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Gọi API để cập nhật profile
                var token = HttpContext.Session.GetString("JWToken");
                Console.WriteLine($"[DEBUG] Calling UpdateProfileAsync with AccountID: {accountId}, Token: {token?.Substring(0, 10)}...");
                var response = await _providerProfileService.UpdateProfileAsync(accountId, model, token);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DEBUG] Profile updated successfully");
                    TempData["Success"] = "Cập nhật profile thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Profile update failed: {response.StatusCode}");
                    TempData["Error"] = "Cập nhật profile thất bại!";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Exception in Edit POST: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        // API: GET /api/provider/profile/{accountId}
        [HttpGet]
        [Route("api/provider/profile/{accountId}")]
        public async Task<IActionResult> GetProfileApi(string accountId)
        {
            try
            {
                var profile = await _providerProfileService.GetProfileAsync(accountId);
                return Json(profile);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = "Provider not found" });
            }
        }

        // API: PUT /api/provider/profile/{accountId}
        [HttpPut]
        [Route("api/provider/profile/{accountId}")]
        public async Task<IActionResult> UpdateProfileApi(string accountId, [FromBody] ProviderProfileUpdateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _providerProfileService.UpdateProfileAsync(accountId, model);
                
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { message = "Profile updated successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Update failed" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API: GET /api/provider/get-id-by-account/{accountId}
        [HttpGet]
        [Route("api/provider/get-id-by-account/{accountId}")]
        public async Task<IActionResult> GetProviderIdByAccount(string accountId)
        {
            try
            {
                var providerId = await _providerProfileService.GetProviderIdByAccountAsync(accountId);
                return Json(providerId);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = "Không tìm thấy Provider cho tài khoản này." });
            }
        }

        // GET: Provider/ProviderProfile/ChangePassword
        public IActionResult ChangePassword()
        {
            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                return View(new ChangePasswordDTO());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Provider/ProviderProfile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
        {
            try
            {
                var accountId = HttpContext.Session.GetString("AccountID");
                if (string.IsNullOrEmpty(accountId))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Gọi API để thay đổi mật khẩu
                var token = HttpContext.Session.GetString("JWToken");
                var response = await _providerProfileService.ChangePasswordAsync(accountId, model, token);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thay đổi mật khẩu thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = "Không thể thay đổi mật khẩu. Vui lòng kiểm tra lại mật khẩu hiện tại.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }
    }
}