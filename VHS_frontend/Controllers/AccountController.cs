using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models.Account;
using VHS_frontend.Services;

namespace VHS_frontend.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginDTO());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDTO model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var result = await _authService.LoginAsync(model);
            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            Console.WriteLine($"[DEBUG] Login success: Token={result.Token}, Role={result.Role}, AccountID={result.AccountID}");

            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("JWTToken", result.Token); // Thêm key này cho consistent
            HttpContext.Session.SetString("Role", result.Role ?? string.Empty);
            HttpContext.Session.SetString("AccountID", result.AccountID.ToString());
            // 🔥 Lưu thêm Username
            HttpContext.Session.SetString("Username", model.Username);

            // ✨ Nếu là Provider, lấy ProviderId từ API
            if (result.Role?.Trim().Equals("Provider", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    var providerIdResult = await _authService.GetProviderIdByAccountIdAsync(result.AccountID.ToString(), result.Token);
                    if (!string.IsNullOrEmpty(providerIdResult))
                    {
                        HttpContext.Session.SetString("ProviderId", providerIdResult);
                        Console.WriteLine($"[DEBUG] ProviderId set in session: {providerIdResult}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to get ProviderId: {ex.Message}");
                }
            }

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Đăng nhập thành công! Xin chào {(result.DisplayName ?? model.Username)} 👋";

            // ✅ Nếu có returnUrl, redirect về đó thay vì redirect theo role
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectByRole(result.Role);
        }


        [HttpGet]
        public IActionResult Register() => View(new RegisterDTO());


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (!ModelState.IsValid) return View(model);

            // Double-check confirm password (phòng khi client-side validate bị tắt)
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp");
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);
            if (result == null || !result.Success)
            {
                // Hiển thị message từ API nếu có
                ModelState.AddModelError(string.Empty, result?.Message ?? "Đăng ký thất bại");
                return View(model);
            }

            // Lưu email vào session để dùng cho OTP verification
            HttpContext.Session.SetString("PendingActivationEmail", model.Email);
            TempData["ShowOTPModal"] = true;
            TempData["RegisterMessage"] = result.Message ?? "Đăng ký thành công. Vui lòng kiểm tra email để lấy mã OTP.";
            
            return View(model); // Giữ lại view để hiển thị modal OTP
        }

        [HttpPost]
        public async Task<IActionResult> ActivateAccount([FromBody] VerifyOTPDTO dto)
        {
            var result = await _authService.ActivateAccountAsync(dto.Email, dto.OTP);
            if (result?.Success == true)
            {
                HttpContext.Session.Remove("PendingActivationEmail");
                TempData.Remove("ShowOTPModal"); // Xóa TempData để tránh hiển thị lại khi reload
                return Json(new { success = true, message = result.Message });
            }
            return Json(new { success = false, message = result?.Message ?? "Kích hoạt thất bại." });
        }

        [HttpPost]
        public async Task<IActionResult> ResendOTP([FromBody] ResendOTPDTO dto)
        {
            var result = await _authService.ResendOTPAsync(dto.Email);
            if (result?.Success == true)
            {
                return Json(new { success = true, message = result.Message });
            }
            return Json(new { success = false, message = result?.Message ?? "Gửi lại OTP thất bại." });
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Helper redirect theo Role
        private IActionResult RedirectByRole(string? role)
        {
            var normalized = (role ?? "").Trim().ToLowerInvariant();
            return normalized switch
            {
                "admin" => RedirectToAction("Index", "AdminDashboard", new { area = "Admin" }),
                "provider" => RedirectToAction("Index", "ProviderDashboard", new { area = "Provider" }),
                "customer" => RedirectToAction("Index", "Home", new { area = "Customer" }),
                _ => RedirectToAction("Index", "Home")
            };
        }

        public IActionResult Profile()
        {
            // TODO: lấy thông tin cá nhân từ API
            return View();
        }

        public IActionResult History()
        {
            // TODO: lấy lịch sử dịch vụ từ API
            return View();
        }


        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Gửi email reset password
            // Giả sử bạn có service EmailSender -> gọi emailSender.SendResetLink(model.Email);

            TempData["Success"] = "Liên kết đặt lại mật khẩu đã được gửi đến email của bạn.";
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordDTO
            {
                Email = email,
                Token = token
            };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // TODO: xử lý logic reset password bằng token + email
            // Ví dụ: gọi service.ResetPassword(model.Email, model.Token, model.Password);

            TempData["Success"] = "Mật khẩu của bạn đã được đặt lại thành công!";
            return RedirectToAction("Login", "Account");
        }
    }
}
