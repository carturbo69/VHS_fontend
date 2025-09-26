using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VHS_fontend.Models;

namespace VHS_fontend.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult About() => View();

        public IActionResult Contact() => View();
    }
}
