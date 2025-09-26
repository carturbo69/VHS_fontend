using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services;
using VHS_fontend.Models.Account;

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


            // Lưu session
            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("Role", result.Role ?? string.Empty);
            HttpContext.Session.SetString("AccountID", result.AccountID.ToString());

            // Redirect theo Role
            return RedirectByRole(result.Role);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterDTO());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.RegisterAsync(model);
            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "Đăng ký thất bại");
                return View(model);
            }

            // Sau khi đăng ký → sang login
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
                "provider" => RedirectToAction("Index", "Home", new { area = "Provider" }),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
