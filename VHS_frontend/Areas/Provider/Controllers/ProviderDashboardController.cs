using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Dashboard;
using System.Text.Json;
using VHS_frontend.Areas.Provider.Models.Schedule;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderDashboardController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProviderDashboardController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private string GetApiBaseUrl()
        {
            return _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5154";
        }

        private string? GetJwtToken()
        {
            return HttpContext.Session.GetString("JWToken");
        }

        public async Task<IActionResult> Index()
        {
            // Tạo model cho dashboard với thông tin mẫu
            var model = new ProviderDashboardViewModel
            {
                ProviderName = HttpContext.Session.GetString("ProviderName") ?? "Provider Demo",
                TotalServices = 12,
                ActiveOrders = 8,
                CompletedOrders = 156,
                PendingBookings = 5,
                MonthlyRevenue = 12500000, // 12.5 triệu VND
                RecentOrders = new List<RecentOrderViewModel>
                {
                    new RecentOrderViewModel
                    {
                        OrderId = "ORD001",
                        CustomerName = "Nguyễn Văn A",
                        ServiceName = "Vệ sinh nhà cửa",
                        OrderDate = DateTime.Now.AddHours(-2),
                        Status = "Đang thực hiện",
                        Amount = 500000
                    },
                    new RecentOrderViewModel
                    {
                        OrderId = "ORD002", 
                        CustomerName = "Trần Thị B",
                        ServiceName = "Sửa chữa điện nước",
                        OrderDate = DateTime.Now.AddHours(-5),
                        Status = "Hoàn thành",
                        Amount = 800000
                    },
                    new RecentOrderViewModel
                    {
                        OrderId = "ORD003",
                        CustomerName = "Lê Văn C", 
                        ServiceName = "Dọn dẹp văn phòng",
                        OrderDate = DateTime.Now.AddDays(-1),
                        Status = "Đã thanh toán",
                        Amount = 1200000
                    }
                },
                MonthlyStats = new MonthlyStatsViewModel
                {
                    January = 8500000,
                    February = 9200000,
                    March = 7800000,
                    April = 11200000,
                    May = 12500000
                },
                // Load Schedule Overview (optional, if fails return null)
                ScheduleOverview = await GetScheduleOverviewAsync()
            };

            ViewData["Title"] = "Dashboard";
            return View(model);
        }

        private async Task<ScheduleOverviewViewModel?> GetScheduleOverviewAsync()
        {
            try
            {
                var jwt = GetJwtToken();
                if (string.IsNullOrEmpty(jwt)) return null;

                using var request = new HttpRequestMessage(HttpMethod.Get, $"{GetApiBaseUrl()}/api/ProviderSchedule/overview");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Schedule API Error: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (apiResult != null && apiResult.TryGetValue("data", out var dataObj))
                {
                    var dataJson = JsonSerializer.Serialize(dataObj);
                    return JsonSerializer.Deserialize<ScheduleOverviewViewModel>(dataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetScheduleOverviewAsync Exception: {ex.Message}");
                return null;
            }
        }

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
        }
    }
}
