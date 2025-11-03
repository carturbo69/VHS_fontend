using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Dashboard;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderDashboardController : Controller
    {
        public IActionResult Index()
        {
            // Lấy ProviderId từ Session
            var providerIdStr = HttpContext.Session.GetString("ProviderId");
            Console.WriteLine($"[ProviderDashboard] ProviderId from session: {providerIdStr}");
            
            if (string.IsNullOrEmpty(providerIdStr) || !Guid.TryParse(providerIdStr, out Guid providerId))
            {
                // Nếu không có ProviderId, hiển thị thông báo lỗi hoặc redirect
                TempData["Error"] = "Không tìm thấy thông tin Provider. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            Console.WriteLine($"[ProviderDashboard] Fetching statistics for ProviderId: {providerId}");

            // Lấy thống kê tổng quan từ API (1 lần gọi duy nhất)
            var statistics = await _bookingService.GetProviderStatisticsAsync(providerId);
            
            if (statistics != null)
            {
                Console.WriteLine($"[ProviderDashboard] Statistics:");
                Console.WriteLine($"  - ThisMonthRevenue: {statistics.ThisMonthRevenue:N0} VNĐ");
                Console.WriteLine($"  - TotalRevenue: {statistics.TotalRevenue:N0} VNĐ");
                Console.WriteLine($"  - CompletedCount: {statistics.CompletedCount}");
            }
            else
            {
                Console.WriteLine($"[ProviderDashboard] ⚠️ Statistics is NULL!");
            }

            // Lấy doanh thu theo tháng từ API
            var monthlyRevenue = await _bookingService.GetMonthlyRevenueAsync(providerId);
            
            if (monthlyRevenue != null)
            {
                Console.WriteLine($"[ProviderDashboard] Monthly Revenue:");
                Console.WriteLine($"  - Year: {monthlyRevenue.Year}");
                Console.WriteLine($"  - January: {monthlyRevenue.January:N0}");
                Console.WriteLine($"  - February: {monthlyRevenue.February:N0}");
                Console.WriteLine($"  - March: {monthlyRevenue.March:N0}");
                Console.WriteLine($"  - April: {monthlyRevenue.April:N0}");
                Console.WriteLine($"  - May: {monthlyRevenue.May:N0}");
                Console.WriteLine($"  - June: {monthlyRevenue.June:N0}");
                Console.WriteLine($"  - July: {monthlyRevenue.July:N0}");
                Console.WriteLine($"  - August: {monthlyRevenue.August:N0}");
                Console.WriteLine($"  - September: {monthlyRevenue.September:N0}");
                Console.WriteLine($"  - October: {monthlyRevenue.October:N0}");
                Console.WriteLine($"  - November: {monthlyRevenue.November:N0}");
                Console.WriteLine($"  - December: {monthlyRevenue.December:N0}");
                
                var total = monthlyRevenue.January + monthlyRevenue.February + monthlyRevenue.March +
                           monthlyRevenue.April + monthlyRevenue.May + monthlyRevenue.June +
                           monthlyRevenue.July + monthlyRevenue.August + monthlyRevenue.September +
                           monthlyRevenue.October + monthlyRevenue.November + monthlyRevenue.December;
                Console.WriteLine($"  - TOTAL YEAR: {total:N0} VNĐ");
            }
            else
            {
                Console.WriteLine($"[ProviderDashboard] ⚠️ Monthly Revenue is NULL!");
            }
            
            // Lấy danh sách booking gần đây (5 đơn mới nhất)
            var recentBookingsFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                PageNumber = 1,
                PageSize = 5
            };
            var recentBookingsResult = await _bookingService.GetBookingListAsync(recentBookingsFilter);

            // Tạo model cho dashboard với dữ liệu thực từ API statistics
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
