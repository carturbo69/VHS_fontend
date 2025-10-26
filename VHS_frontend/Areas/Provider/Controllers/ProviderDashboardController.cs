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

            // Tạo model cho dashboard với dữ liệu thực từ API statistics
            var model = new ProviderDashboardViewModel
            {
                ProviderName = HttpContext.Session.GetString("ProviderName") ?? "Provider",
                TotalServices = statistics?.TotalServices ?? 0,
                ActiveOrders = statistics?.ConfirmedCount ?? 0,
                CompletedOrders = statistics?.CompletedCount ?? 0,
                PendingBookings = statistics?.PendingCount ?? 0,
                MonthlyRevenue = statistics?.ThisMonthRevenue ?? 0,
                RecentOrders = recentBookingsResult?.Items?.Select(b => new RecentOrderViewModel
                {
                    OrderId = b.BookingCode,
                    CustomerName = b.CustomerName,
                    ServiceName = b.ServiceName,
                    OrderDate = b.BookingTime,
                    Status = TranslateStatus(b.Status),
                    Amount = b.Amount
                }).ToList() ?? new List<RecentOrderViewModel>(),
                MonthlyStats = monthlyRevenue != null ? new MonthlyStatsViewModel
                {
                    January = monthlyRevenue.January,
                    February = monthlyRevenue.February,
                    March = monthlyRevenue.March,
                    April = monthlyRevenue.April,
                    May = monthlyRevenue.May,
                    June = monthlyRevenue.June,
                    July = monthlyRevenue.July,
                    August = monthlyRevenue.August,
                    September = monthlyRevenue.September,
                    October = monthlyRevenue.October,
                    November = monthlyRevenue.November,
                    December = monthlyRevenue.December
                } : new MonthlyStatsViewModel()
            };

            ViewData["Title"] = "Dashboard";
            return View(model);
        }

        private decimal GetCurrentMonthRevenue(MonthlyRevenueViewModel monthlyRevenue)
        {
            var currentMonth = DateTime.Now.Month;
            return currentMonth switch
            {
                1 => monthlyRevenue.January,
                2 => monthlyRevenue.February,
                3 => monthlyRevenue.March,
                4 => monthlyRevenue.April,
                5 => monthlyRevenue.May,
                6 => monthlyRevenue.June,
                7 => monthlyRevenue.July,
                8 => monthlyRevenue.August,
                9 => monthlyRevenue.September,
                10 => monthlyRevenue.October,
                11 => monthlyRevenue.November,
                12 => monthlyRevenue.December,
                _ => 0
            };
        }

        private string TranslateStatus(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xử lý",
                "Confirmed" => "Đã xác nhận",
                "Completed" => "Hoàn thành",
                "Canceled" => "Đã hủy",
                _ => status
            };
        }
    }
}
