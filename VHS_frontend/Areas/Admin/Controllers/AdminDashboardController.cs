using Microsoft.AspNetCore.Mvc;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            // ✅ Giống Provider: kiểm tra Session ngay trong action
            if (string.IsNullOrEmpty(accountId) ||
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            return View();
        }
    }
}
