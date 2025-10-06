using Microsoft.AspNetCore.Mvc;
using VHS_fontend.Models.Account;
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
                "admin" => RedirectToAction("Index", "Home", new { area = "Admin" }),
                "customer" => RedirectToAction("Index", "Home", new { area = "Customer" }),
                "provider" => RedirectToAction("Index", "HomePage", new { area = "Provider" }),
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

    }
}
