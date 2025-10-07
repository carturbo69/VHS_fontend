using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models;

namespace VHS_frontend.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult About() => View();

        public IActionResult Contact() => View();
    }
}
