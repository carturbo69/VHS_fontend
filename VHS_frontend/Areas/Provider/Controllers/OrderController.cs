using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class OrderController : Controller
    {
        private readonly BookingProviderService _bookingService;
        private readonly StaffManagementService _staffService;

        public OrderController(
            BookingProviderService bookingService,
            StaffManagementService staffService)
        {
            _bookingService = bookingService;
            _staffService = staffService;
        }

        // DEBUG: Kiểm tra session
        [HttpGet]
        public IActionResult DebugSession()
        {
            var providerId = HttpContext.Session.GetString("ProviderId");
            var accountId = HttpContext.Session.GetString("AccountID");
            var token = HttpContext.Session.GetString("JWTToken");
            var role = HttpContext.Session.GetString("Role");

            var debug = new
            {
                ProviderId = providerId ?? "NULL",
                AccountId = accountId ?? "NULL",
                HasToken = !string.IsNullOrEmpty(token),
                Role = role ?? "NULL"
            };

            return Json(debug);
        }

        // GET: Provider/Order/Index
        public async Task<IActionResult> Index(
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Lấy ProviderId từ Session
            var providerIdStr = HttpContext.Session.GetString("ProviderId");
            Console.WriteLine($"[DEBUG] ProviderId from session: {providerIdStr ?? "NULL"}");
            
            if (string.IsNullOrEmpty(providerIdStr) || !Guid.TryParse(providerIdStr, out var providerId))
            {
                Console.WriteLine($"[DEBUG] ProviderId invalid or null - redirecting to login");
                TempData["ErrorMessage"] = "Không tìm thấy thông tin Provider. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var filter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            Console.WriteLine($"[DEBUG] Calling API with ProviderId: {providerId}, Status: {status ?? "NULL"}");
            var result = await _bookingService.GetBookingListAsync(filter);
            Console.WriteLine($"[DEBUG] API returned {result?.Items?.Count ?? 0} bookings, Total: {result?.TotalCount ?? 0}");

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

            // Pass filter data to view
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.PageNumber = pageNumber;

            return View(result);
        }

        // GET: Provider/Order/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var booking = await _bookingService.GetBookingDetailAsync(id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Lấy danh sách staff để hiển thị trong dropdown (nếu cần assign)
            var providerIdStr = HttpContext.Session.GetString("ProviderId");
            var token = HttpContext.Session.GetString("JWTToken");
            
            if (!string.IsNullOrEmpty(providerIdStr))
            {
                try
                {
                    var staffList = await _staffService.GetStaffByProviderAsync(providerIdStr, token);
                    ViewBag.StaffList = staffList ?? new List<VHS_frontend.Areas.Provider.Models.Staff.StaffDTO>();
                }
                catch
                {
                    ViewBag.StaffList = new List<VHS_frontend.Areas.Provider.Models.Staff.StaffDTO>();
                }
            }
            else
            {
                ViewBag.StaffList = new List<VHS_frontend.Areas.Provider.Models.Staff.StaffDTO>();
            }

            return View(booking);
        }

        // POST: Provider/Order/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateBookingStatusDTO dto)
        {
            if (dto == null || dto.BookingId == Guid.Empty)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var success = await _bookingService.UpdateBookingStatusAsync(dto);

            if (success)
            {
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }

            return Json(new { success = false, message = "Cập nhật trạng thái thất bại" });
        }

        // POST: Provider/Order/AssignStaff
        [HttpPost]
        public async Task<IActionResult> AssignStaff([FromBody] AssignStaffDTO dto)
        {
            if (dto == null || dto.BookingId == Guid.Empty || dto.StaffId == Guid.Empty)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var success = await _bookingService.AssignStaffAsync(dto);

            if (success)
            {
                return Json(new { success = true, message = "Phân công nhân viên thành công" });
            }

            return Json(new { success = false, message = "Phân công nhân viên thất bại" });
        }
    }
}

