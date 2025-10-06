using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class HomePageController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomePageController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            var accountId = _httpContextAccessor.HttpContext?.Session.GetString("AccountID");

            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account", new { area = "" });

            // 🔥 Gọi API BE để lấy ProviderID
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:5154/api/Provider/profile/{accountId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                var providerId = data["providerId"]?.ToString();

                if (!string.IsNullOrEmpty(providerId))
                    _httpContextAccessor.HttpContext?.Session.SetString("ProviderID", providerId);
            }

            ViewBag.AccountID = accountId;
            ViewBag.ProviderID = _httpContextAccessor.HttpContext?.Session.GetString("ProviderID");

            return View();
        }
    }
}
