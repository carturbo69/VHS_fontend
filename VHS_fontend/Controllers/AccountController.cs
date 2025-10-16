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
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(model);
            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu hoặc máy chủ không phản hồi.");
                return View(model);
            }

            Console.WriteLine($"[DEBUG] Login success: Token={result.Token}, Role={result.Role}, AccountID={result.AccountID}");

            // ✅ Lưu session cơ bản
            HttpContext.Session.SetString("JWToken", result.Token);
            HttpContext.Session.SetString("Role", result.Role ?? string.Empty);
            HttpContext.Session.SetString("AccountID", result.AccountID.ToString());
            HttpContext.Session.SetString("Username", model.Username);

            // 🟢 Nếu user là Provider → lấy ProviderID
            if (result.Role?.ToLower() == "provider")
            {
                try
                {
                    var provider = await _authService.GetProviderProfileByAccountIdAsync(result.AccountID);
                    if (provider != null)
                    {
                        HttpContext.Session.SetString("ProviderID", provider.ProviderId.ToString());
                        Console.WriteLine($"[SESSION SAVED] ProviderID = {provider.ProviderId}");
                    }
                    else
                    {
                        Console.WriteLine("[WARN] Không tìm thấy Provider profile cho tài khoản này!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Khi lấy Provider profile: {ex.Message}");
                }
            }

            // 🧩 Log xác nhận session
            Console.WriteLine($"[SESSION] AccountID={HttpContext.Session.GetString("AccountID")}");
            Console.WriteLine($"[SESSION] ProviderID={HttpContext.Session.GetString("ProviderID")}");
            Console.WriteLine($"[SESSION] Role={HttpContext.Session.GetString("Role")}");

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
            if (!ModelState.IsValid)
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp");
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);

            if (result == null || !result.Success)
            {
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

        private IActionResult RedirectByRole(string? role)
        {
            var normalized = (role ?? "").Trim().ToLowerInvariant();
            Console.WriteLine($"[REDIRECT] Role={normalized}");

            return normalized switch
            {
                "admin" => RedirectToAction("Index", "Home", new { area = "Admin" }),
                "customer" => RedirectToAction("Index", "Home", new { area = "Customer" }),
                "provider" => RedirectToAction("Index", "HomePage", new { area = "Provider" }),
                _ => RedirectToAction("Index", "Home")
            };
        }

        public IActionResult Profile() => View();
        public IActionResult History() => View();
    }
}
