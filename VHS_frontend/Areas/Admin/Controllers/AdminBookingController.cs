using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Booking;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBookingController : Controller
    {
        private readonly AdminBookingService _bookingService;

        public AdminBookingController(AdminBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // GET: Admin/AdminBooking/Index
        // Hiển thị TẤT CẢ bookings của TẤT CẢ providers (không filter theo provider)
        // Admin chỉ được XEM, không được chỉnh sửa
        public async Task<IActionResult> Index(
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Lấy token từ session
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            _bookingService.SetBearerToken(token);

            var filter = new AdminBookingFilterDTO
            {
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _bookingService.GetAllBookingsAsync(filter);

            if (result == null)
            {
                result = new BookingListResultDTO
                {
                    Items = new List<BookingListItemDTO>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = 10
                };
            }

            // ✨ THỐNG KÊ THÁNG NÀY - Lấy từng loại riêng biệt
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1);
            
            // Đếm Pending
            var pendingFilter = new AdminBookingFilterDTO
            {
                Status = "Pending",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var pendingData = await _bookingService.GetAllBookingsAsync(pendingFilter);
            ViewBag.MonthPending = pendingData?.TotalCount ?? 0;
            
            // Đếm Confirmed
            var confirmedFilter = new AdminBookingFilterDTO
            {
                Status = "Confirmed",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var confirmedData = await _bookingService.GetAllBookingsAsync(confirmedFilter);
            ViewBag.MonthConfirmed = confirmedData?.TotalCount ?? 0;
            
            // Đếm Completed
            var completedFilter = new AdminBookingFilterDTO
            {
                Status = "Completed",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var completedData = await _bookingService.GetAllBookingsAsync(completedFilter);
            ViewBag.MonthCompleted = completedData?.TotalCount ?? 0;
            
            // Đếm Canceled
            var canceledFilter = new AdminBookingFilterDTO
            {
                Status = "Canceled",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var canceledData = await _bookingService.GetAllBookingsAsync(canceledFilter);
            ViewBag.MonthCanceled = canceledData?.TotalCount ?? 0;

            // Pass filter data to view
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.PageNumber = pageNumber;

            return View(result);
        }

        // GET: Admin/AdminBooking/Details/5
        // Xem chi tiết booking - CHỈ XEM, không có quyền chỉnh sửa (update status, assign staff, cancel)
        public async Task<IActionResult> Details(Guid id)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            _bookingService.SetBearerToken(token);

            try
            {
                var booking = await _bookingService.GetBookingDetailAsync(id);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải chi tiết đơn hàng: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

