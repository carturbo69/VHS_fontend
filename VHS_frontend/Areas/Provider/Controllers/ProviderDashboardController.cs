using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Dashboard;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderDashboardController : Controller
    {
        private readonly BookingProviderService _bookingService;

        public ProviderDashboardController(BookingProviderService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index()
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

            // Map dữ liệu thực vào ViewModel (không còn dữ liệu cứng)
            var recentOrders = new List<RecentOrderViewModel>();
            if (recentBookingsResult?.Items != null && recentBookingsResult.Items.Any())
            {
                recentOrders = recentBookingsResult.Items
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(5)
                    .Select(i => new RecentOrderViewModel
                    {
                        OrderId = i.BookingCode,
                        CustomerName = i.CustomerName,
                        ServiceName = i.ServiceName,
                        OrderDate = i.CreatedAt,
                        Status = i.Status,
                        Amount = i.Amount
                    }).ToList();
            }

            var model = new ProviderDashboardViewModel
            {
                ProviderName = HttpContext.Session.GetString("ProviderName") ?? "Provider",
                TotalServices = statistics?.TotalServices ?? 0,
                ActiveOrders = statistics?.ConfirmedCount ?? 0,
                CompletedOrders = statistics?.CompletedCount ?? 0,
                PendingBookings = statistics?.PendingCount ?? 0,
                MonthlyRevenue = statistics?.ThisMonthRevenue ?? 0,
                RecentOrders = recentOrders,
                MonthlyStats = new MonthlyStatsViewModel
                {
                    January = monthlyRevenue?.January ?? 0,
                    February = monthlyRevenue?.February ?? 0,
                    March = monthlyRevenue?.March ?? 0,
                    April = monthlyRevenue?.April ?? 0,
                    May = monthlyRevenue?.May ?? 0,
                    June = monthlyRevenue?.June ?? 0,
                    July = monthlyRevenue?.July ?? 0,
                    August = monthlyRevenue?.August ?? 0,
                    September = monthlyRevenue?.September ?? 0,
                    October = monthlyRevenue?.October ?? 0,
                    November = monthlyRevenue?.November ?? 0,
                    December = monthlyRevenue?.December ?? 0
                }
            };

            ViewData["Title"] = "Dashboard";
            return View(model);
        }
    }
}
