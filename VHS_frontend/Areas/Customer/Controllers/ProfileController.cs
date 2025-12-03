using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VHS_frontend.Areas.Customer.Models.Profile;
using VHS_frontend.Services.Customer;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProfileController : Controller
    {
        private readonly ProfileServiceCustomer _profileService;
        private readonly BookingServiceCustomer _bookingServiceCustomer;
        private readonly ReviewServiceCustomer _reviewServiceCustomer;

        public ProfileController(
            ProfileServiceCustomer profileService,
            BookingServiceCustomer bookingServiceCustomer,
            ReviewServiceCustomer reviewServiceCustomer)
        {
            _profileService = profileService;
            _bookingServiceCustomer = bookingServiceCustomer;
            _reviewServiceCustomer = reviewServiceCustomer;
        }

        /// <summary>
        /// Hiển thị trang profile của customer
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để xem profile.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                System.Diagnostics.Debug.WriteLine($"[ProfileController] Getting profile with token: {jwtToken?.Substring(0, 20)}...");
                
                var profile = await _profileService.GetProfileAsync(jwtToken);
                
                System.Diagnostics.Debug.WriteLine($"[ProfileController] Profile result: {(profile == null ? "NULL" : profile.AccountName)}");
                
                if (profile == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ProfileController] Profile is NULL - Redirecting to Home");
                    TempData["ToastError"] = "Không thể tải thông tin profile.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                // Lấy số đơn hàng đã hoàn thành
                int completedOrdersCount = 0;
                try
                {
                    var bookingHistory = await _bookingServiceCustomer.GetHistoryByAccountAsync(accountId, jwtToken);
                    if (bookingHistory?.Items != null)
                    {
                        completedOrdersCount = bookingHistory.Items.Count(b => 
                            b.Status != null && 
                            (b.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                             b.Status.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase)));
                    }
                }
                catch
                {
                    // Nếu lỗi thì giữ giá trị mặc định 0
                }

                // Lấy số đánh giá đã viết
                int reviewsCount = 0;
                try
                {
                    var (success, reviews, _) = await _reviewServiceCustomer.GetMyReviewsAsync(accountId, jwtToken);
                    if (success && reviews != null)
                    {
                        reviewsCount = reviews.Count;
                    }
                }
                catch
                {
                    // Nếu lỗi thì giữ giá trị mặc định 0
                }

                var viewModel = new ProfileViewModel
                {
                    UserId = profile.UserId,
                    AccountId = profile.AccountId,
                    AccountName = profile.AccountName,
                    Email = profile.Email,
                    Role = profile.Role,
                    FullName = profile.FullName ?? "",
                    PhoneNumber = profile.PhoneNumber ?? "",
                    Images = profile.Images,
                    Address = profile.Address ?? "",
                    CreatedAt = profile.CreatedAt,
                    UpdatedAt = profile.UpdatedAt,
                    IsProfileComplete = profile.IsProfileComplete,
                    CompletedOrdersCount = completedOrdersCount,
                    ReviewsCount = reviewsCount
                };

                return View(viewModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Phiên đăng nhập đã hết hạn: {ex.Message}";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi khi tải profile: {ex.Message}";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa profile
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để chỉnh sửa profile.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                var profile = await _profileService.GetProfileAsync(jwtToken);
                
                if (profile == null)
                {
                    TempData["ToastError"] = "Không thể tải thông tin profile.";
                    return RedirectToAction(nameof(Index));
                }

                var editModel = new EditProfileViewModel
                {
                    AccountName = profile.AccountName,
                    Email = profile.Email,
                    FullName = profile.FullName ?? "",
                    PhoneNumber = profile.PhoneNumber ?? "",
                    Address = profile.Address ?? "",
                    CurrentImage = profile.Images
                };

                return View(editModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Phiên đăng nhập đã hết hạn: {ex.Message}";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi khi tải profile: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Xử lý cập nhật profile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để chỉnh sửa profile.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                var editDto = new EditProfileDTO
                {
                    AccountName = model.AccountName,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Images = model.CurrentImage // Giữ nguyên ảnh hiện tại
                };

                var result = await _profileService.UpdateProfileAsync(editDto, jwtToken);
                
                if (result.Success)
                {
                    TempData["ToastSuccess"] = "Cập nhật profile thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ToastError"] = result.Message ?? "Có lỗi xảy ra khi cập nhật profile.";
                    return View(model);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Phiên đăng nhập đã hết hạn: {ex.Message}";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi khi cập nhật profile: {ex.Message}";
                return View(model);
            }
        }

        /// <summary>
        /// API: Update profile from modal
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromForm] EditProfileViewModel model)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            // Validate phone number if provided
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                // Remove spaces and special characters for validation
                var cleanedPhone = model.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
                
                // Vietnamese phone number patterns
                // 10 digits starting with 0 (e.g., 0123456789)
                // 11-12 characters starting with +84 (e.g., +84123456789)
                var phonePattern = new System.Text.RegularExpressions.Regex(@"^(0[0-9]{9}|\+84[0-9]{9,10})$");
                
                if (!phonePattern.IsMatch(cleanedPhone))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ. Vui lòng nhập 10 số bắt đầu bằng 0 (ví dụ: 0123456789) hoặc +84 (ví dụ: +84123456789)" });
                }
                
                // Normalize phone number (use cleaned version)
                model.PhoneNumber = cleanedPhone;
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                
                var editDto = new EditProfileDTO
                {
                    AccountName = model.AccountName,
                    Email = model.Email,  // Keep current email, actual change via ChangeEmail action
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address
                    // Images is updated separately via UploadImage
                };

                var result = await _profileService.UpdateProfileAsync(editDto, jwtToken);
                
                if (result.Success)
                {
                    return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message ?? "Có lỗi xảy ra khi cập nhật." });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để đổi mật khẩu.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View(new ChangePasswordViewModel());
        }

        /// <summary>
        /// Request OTP để đổi mật khẩu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RequestPasswordChangeOTP()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine("[ProfileController] RequestPasswordChangeOTP: No accountId found");
                return Json(new { success = false, message = "Bạn cần đăng nhập để đổi mật khẩu." });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestPasswordChangeOTP: JWT Token exists: {!string.IsNullOrEmpty(jwtToken)}");
                
                if (string.IsNullOrEmpty(jwtToken))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
                }

                var result = await _profileService.RequestPasswordChangeOTPAsync(jwtToken);
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestPasswordChangeOTP result: Success={result.Success}, Message={result.Message}");
                
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestPasswordChangeOTP UnauthorizedAccessException: {ex.Message}");
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestPasswordChangeOTP Exception: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi khi gửi OTP: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xử lý đổi mật khẩu (View version - deprecated, use API version below)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordView(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ChangePassword", model);
            }

            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để đổi mật khẩu.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                
                var changeDto = new ChangePasswordDTO
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword,
                    OTP = model.OTP
                };

                var result = await _profileService.ChangePasswordAsync(changeDto, jwtToken);
                
                if (result.Success)
                {
                    TempData["ToastSuccess"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ToastError"] = result.Message ?? "Có lỗi xảy ra khi đổi mật khẩu.";
                    return View("ChangePassword", model);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Phiên đăng nhập đã hết hạn: {ex.Message}";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi khi đổi mật khẩu: {ex.Message}";
                return View("ChangePassword", model);
            }
        }

        /// <summary>
        /// API: Change password from modal
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel model)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                
                var changeDto = new ChangePasswordDTO
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword,
                    OTP = model.OTP
                };

                var result = await _profileService.ChangePasswordAsync(changeDto, jwtToken);
                
                if (result.Success)
                {
                    return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message ?? "Có lỗi xảy ra khi đổi mật khẩu." });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Hiển thị form đổi email
        /// </summary>
        [HttpGet]
        public IActionResult ChangeEmail()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để đổi email.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View(new ChangeEmailViewModel());
        }

        /// <summary>
        /// Request OTP để đổi email
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RequestEmailChangeOTP()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine("[ProfileController] RequestEmailChangeOTP: No accountId found");
                return Json(new { success = false, message = "Bạn cần đăng nhập để đổi email." });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestEmailChangeOTP: JWT Token exists: {!string.IsNullOrEmpty(jwtToken)}");
                
                if (string.IsNullOrEmpty(jwtToken))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
                }

                var result = await _profileService.RequestEmailChangeOTPAsync(jwtToken);
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestEmailChangeOTP result: Success={result.Success}, Message={result.Message}");
                
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestEmailChangeOTP UnauthorizedAccessException: {ex.Message}");
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileController] RequestEmailChangeOTP Exception: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi khi gửi OTP: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xử lý đổi email (View version - deprecated, use API version below)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmailView(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ChangeEmail", model);
            }

            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập để đổi email.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                
                var changeDto = new ChangeEmailDTO
                {
                    NewEmail = model.NewEmail,
                    OtpCode = model.OTP
                };

                var result = await _profileService.ChangeEmailAsync(changeDto, jwtToken);
                
                if (result.Success)
                {
                    TempData["ToastSuccess"] = "Đổi email thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ToastError"] = result.Message ?? "Có lỗi xảy ra khi đổi email.";
                    return View("ChangeEmail", model);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Phiên đăng nhập đã hết hạn: {ex.Message}";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi khi đổi email: {ex.Message}";
                return View("ChangeEmail", model);
            }
        }

        /// <summary>
        /// API: Change email from modal
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangeEmail([FromForm] ChangeEmailViewModel model)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            // Clean OTP before validation to avoid ModelState errors
            if (!string.IsNullOrWhiteSpace(model.OTP))
            {
                model.OTP = model.OTP.Trim().Replace(" ", "").Replace("-", "").Replace(".", "");
            }

            // Clear OTP validation errors from ModelState and re-validate manually
            ModelState.Remove("OTP");
            
            // Validate OTP
            if (string.IsNullOrWhiteSpace(model.OTP))
            {
                ModelState.AddModelError("OTP", "Mã OTP là bắt buộc");
            }
            else if (model.OTP.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(model.OTP, @"^\d{6}$"))
            {
                ModelState.AddModelError("OTP", "Mã OTP phải có đúng 6 chữ số");
            }

            // Validate ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                
                // Translate common validation errors to Vietnamese
                var translatedErrors = errors.Select(e =>
                {
                    if (e.Contains("OTP") && e.Contains("required") || e.Contains("không được để trống"))
                        return "Mã OTP là bắt buộc";
                    if (e.Contains("OTP") && (e.Contains("6") || e.Contains("ký tự")))
                        return "Mã OTP phải có đúng 6 chữ số";
                    if (e.Contains("Email") && e.Contains("required") || e.Contains("không được để trống"))
                        return "Email mới là bắt buộc";
                    if (e.Contains("Email") && e.Contains("invalid") || e.Contains("không hợp lệ"))
                        return "Email không hợp lệ";
                    return e;
                }).ToList();
                
                return Json(new { success = false, message = string.Join(", ", translatedErrors), errors = translatedErrors });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                
                // OTP has already been cleaned above, just verify it's valid
                if (string.IsNullOrWhiteSpace(model.OTP))
                {
                    return Json(new { success = false, message = "Mã OTP là bắt buộc" });
                }
                
                // Check if NewEmail is provided
                if (string.IsNullOrWhiteSpace(model.NewEmail))
                {
                    return Json(new { success = false, message = "Email mới là bắt buộc" });
                }
                
                var changeDto = new ChangeEmailDTO
                {
                    NewEmail = model.NewEmail?.Trim(),
                    OtpCode = model.OTP?.Trim()  // Map OTP from ViewModel to OtpCode in DTO
                };

                var result = await _profileService.ChangeEmailAsync(changeDto, jwtToken);
                
                if (result.Success)
                {
                    return Json(new { success = true, message = "Đổi email thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message ?? "Có lỗi xảy ra khi đổi email." });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Upload ảnh profile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để upload ảnh." });
            }

            if (image == null || image.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ảnh để upload." });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                var result = await _profileService.UploadProfileImageAsync(image, jwtToken);
                
                return Json(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi upload ảnh: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xóa ảnh profile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để xóa ảnh." });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                var result = await _profileService.DeleteProfileImageAsync(jwtToken);
                
                return Json(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa ảnh: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lấy thông tin profile completeness
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfileCompleteness()
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                var result = await _profileService.CheckProfileCompletenessAsync(jwtToken);
                
                return Json(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = $"Phiên đăng nhập đã hết hạn: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi kiểm tra profile: {ex.Message}" });
            }
        }

        /// <summary>
        /// Helper: Lấy AccountId từ claims hoặc session
        /// </summary>
        private Guid GetAccountId()
        {
            var idStr = User.FindFirstValue("AccountID") ?? HttpContext.Session.GetString("AccountID");
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }
    }
}
