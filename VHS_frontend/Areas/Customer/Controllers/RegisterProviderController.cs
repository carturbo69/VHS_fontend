using Microsoft.AspNetCore.Mvc;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class RegisterProviderController : Controller
    {
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Categories = new[]
            {
                new { Id = "1", Name = "Sửa chữa điện" },
                new { Id = "2", Name = "Vệ sinh nhà cửa" },
                new { Id = "3", Name = "Sơn sửa" },
                new { Id = "4", Name = "Sửa chữa điện nước" },
                new { Id = "5", Name = "Làm vườn" },
                new { Id = "6", Name = "Sửa chữa điều hòa" },
                new { Id = "7", Name = "Sửa chữa máy giặt" },
                new { Id = "8", Name = "Dọn dẹp" },
            };
            return View();
        }

        // fallback submit (giả lập)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterFallback()
        {
            await Task.Delay(800);
            TempData["Ok"] = "Đăng ký (giả lập) thành công!";
            return RedirectToAction(nameof(Register));
        }
    }
}
