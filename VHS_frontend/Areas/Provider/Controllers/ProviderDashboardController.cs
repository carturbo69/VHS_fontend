using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Dashboard;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderDashboardController : Controller
    {
        public IActionResult Index()
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
                }
            };

            ViewData["Title"] = "Dashboard";
            return View(model);
        }
    }
}
