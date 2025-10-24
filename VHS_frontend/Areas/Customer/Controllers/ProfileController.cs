using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VHS_frontend.Areas.Customer.Models.Profile;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProfileController : Controller
    {
        private readonly ProfileServiceCustomer _profileService;

        public ProfileController(ProfileServiceCustomer profileService)
        {
            _profileService = profileService;
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
                var profile = await _profileService.GetProfileAsync(jwtToken);
                
                if (profile == null)
                {
                    TempData["ToastError"] = "Không thể tải thông tin profile.";
                    return RedirectToAction("Index", "Home", new { area = "" });
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
                    IsProfileComplete = profile.IsProfileComplete
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
        /// Xử lý đổi mật khẩu
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
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
                
                // Bước 1: Request OTP
                var otpResult = await _profileService.RequestPasswordChangeOTPAsync(jwtToken);
                if (!otpResult.Success)
                {
                    TempData["ToastError"] = otpResult.Message ?? "Không thể gửi OTP.";
                    return View(model);
                }

                // Bước 2: Change password với OTP
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
                TempData["ToastError"] = $"Lỗi khi đổi mật khẩu: {ex.Message}";
                return View(model);
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
        /// Xử lý đổi email
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
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
                
                // Bước 1: Request OTP
                var otpResult = await _profileService.RequestEmailChangeOTPAsync(jwtToken);
                if (!otpResult.Success)
                {
                    TempData["ToastError"] = otpResult.Message ?? "Không thể gửi OTP.";
                    return View(model);
                }

                // Bước 2: Change email với OTP
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
                TempData["ToastError"] = $"Lỗi khi đổi email: {ex.Message}";
                return View(model);
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
