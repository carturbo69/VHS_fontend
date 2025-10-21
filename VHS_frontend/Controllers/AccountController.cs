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
        public IActionResult Login() => View(new LoginDTO());


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.LoginAsync(model);
            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }

            Console.WriteLine($"[DEBUG] Login success: Token={result.Token}, Role={result.Role}, AccountID={result.AccountID}");

            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("Role", result.Role ?? string.Empty);
            HttpContext.Session.SetString("AccountID", result.AccountID.ToString());
            // 🔥 Lưu thêm Username
            HttpContext.Session.SetString("Username", model.Username);

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Đăng nhập thành công! Xin chào {(result.DisplayName ?? model.Username)} 👋";

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

            TempData["RegisterMessage"] = result.Message ?? "Đăng ký thành công";
            return RedirectToAction("Login");
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
