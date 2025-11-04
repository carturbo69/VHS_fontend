using Microsoft.AspNetCore.Mvc;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class HomePageController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Trang chủ Provider";
            return View();
        }
    }
}
