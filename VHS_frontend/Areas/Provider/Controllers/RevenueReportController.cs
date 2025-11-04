using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Dashboard;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    [Route("Provider/Revenue")]
    public class RevenueReportController : Controller
    {
        private readonly BookingProviderService _bookingService;

        public RevenueReportController(BookingProviderService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? month, int? year)
        {
            // Lấy ProviderId từ Session
            var providerIdStr = HttpContext.Session.GetString("ProviderId");
            if (string.IsNullOrEmpty(providerIdStr) || !Guid.TryParse(providerIdStr, out Guid providerId))
            {
                TempData["Error"] = "Không tìm thấy thông tin Provider. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Mặc định: tháng và năm hiện tại
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var filter = new RevenueReportFilterViewModel
            {
                Month = selectedMonth,
                Year = selectedYear
            };

            Console.WriteLine($"[RevenueReport] ProviderId: {providerId}, Month: {selectedMonth}, Year: {selectedYear}");
            var report = await _bookingService.GetRevenueReportAsync(providerId, filter);
            Console.WriteLine($"[RevenueReport] Report received: {(report != null ? $"Revenue={report.TotalRevenue}, Count={report.TotalCompletedBookings}" : "NULL")}");

            if (report == null)
            {
                // Tạo báo cáo rỗng nếu không có dữ liệu
                report = new RevenueReportViewModel
                {
                    FromDate = new DateTime(selectedYear, selectedMonth, 1),
                    ToDate = new DateTime(selectedYear, selectedMonth, 1).AddMonths(1).AddDays(-1),
                    TotalRevenue = 0,
                    TotalCompletedBookings = 0,
                    AverageOrderValue = 0,
                    TopServices = new List<TopServiceRevenueViewModel>(),
                    Details = new List<RevenueDetailItemViewModel>(),
                    DailyRevenues = new List<DailyRevenueViewModel>()
                };
            }

            ViewData["Title"] = "Báo cáo doanh thu";
            ViewData["SelectedMonth"] = selectedMonth;
            ViewData["SelectedYear"] = selectedYear;
            
            return View(report);
        }
    }
}

