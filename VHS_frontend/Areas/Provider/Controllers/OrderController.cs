using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Areas.Provider.Models.Staff;
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

            // ✨ THỐNG KÊ THÁNG NÀY - Lấy từng loại riêng biệt
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1);
            
            // Đếm Pending
            var pendingFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Pending",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var pendingData = await _bookingService.GetBookingListAsync(pendingFilter);
            ViewBag.MonthPending = pendingData?.TotalCount ?? 0;
            
            // Đếm Confirmed
            var confirmedFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Confirmed",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var confirmedData = await _bookingService.GetBookingListAsync(confirmedFilter);
            ViewBag.MonthConfirmed = confirmedData?.TotalCount ?? 0;
            
            // Đếm Completed
            var completedFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Completed",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var completedData = await _bookingService.GetBookingListAsync(completedFilter);
            ViewBag.MonthCompleted = completedData?.TotalCount ?? 0;
            
            // Đếm Canceled
            var canceledFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Canceled",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1
            };
            var canceledData = await _bookingService.GetBookingListAsync(canceledFilter);
            ViewBag.MonthCanceled = canceledData?.TotalCount ?? 0;

            // Pass filter data to view
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.PageNumber = pageNumber;

            return View(result);
        }

        // GET: Provider/Order/History - Lịch sử đơn hàng (Completed + Canceled)
        public async Task<IActionResult> History(
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
        {
            // Lấy ProviderId từ Session
            var providerIdStr = HttpContext.Session.GetString("ProviderId");
            Console.WriteLine($"[History] ProviderId from session: {providerIdStr ?? "NULL"}");
            
            if (string.IsNullOrEmpty(providerIdStr) || !Guid.TryParse(providerIdStr, out var providerId))
            {
                Console.WriteLine($"[History] ProviderId invalid - redirecting to login");
                TempData["ErrorMessage"] = "Không tìm thấy thông tin Provider. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // ✅ GỌI API ĐÚNG: Lấy riêng Completed và Canceled
            Console.WriteLine($"[History] Calling API with ProviderId: {providerId}, Status filter: {status ?? "ALL"}");

            List<BookingListItemDTO> allItems = new List<BookingListItemDTO>();

            if (string.IsNullOrEmpty(status) || status == "Completed")
            {
                // Lấy Completed
                var completedFilter = new BookingFilterDTO
                {
                    ProviderId = providerId,
                    Status = "Completed",
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchTerm = searchTerm,
                    PageNumber = 1,
                    PageSize = 1000
                };
                var completedResult = await _bookingService.GetBookingListAsync(completedFilter);
                if (completedResult?.Items != null)
                {
                    allItems.AddRange(completedResult.Items);
                    Console.WriteLine($"[History] Got {completedResult.Items.Count} Completed bookings");
                }
            }

            if (string.IsNullOrEmpty(status) || status == "Canceled")
            {
                // Lấy Canceled
                var canceledFilter = new BookingFilterDTO
                {
                    ProviderId = providerId,
                    Status = "Canceled",
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchTerm = searchTerm,
                    PageNumber = 1,
                    PageSize = 1000
                };
                var canceledResult = await _bookingService.GetBookingListAsync(canceledFilter);
                if (canceledResult?.Items != null)
                {
                    allItems.AddRange(canceledResult.Items);
                    Console.WriteLine($"[History] Got {canceledResult.Items.Count} Canceled bookings");
                }
            }

            Console.WriteLine($"[History] Total history bookings: {allItems.Count}");

            // Sort by date desc
            allItems = allItems.OrderByDescending(b => b.CreatedAt).ToList();

            var result = new BookingListResultDTO
            {
                Items = allItems,
                TotalCount = allItems.Count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // ✨ THỐNG KÊ THÁNG NÀY CHO LỊCH SỬ - Gọi API riêng cho từng status
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1);
            
            // Đếm Completed tháng này
            var completedMonthFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Completed",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1 // Chỉ cần đếm, không cần lấy data
            };
            var completedMonthData = await _bookingService.GetBookingListAsync(completedMonthFilter);
            ViewBag.MonthCompleted = completedMonthData?.TotalCount ?? 0;
            
            // Đếm Canceled tháng này
            var canceledMonthFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Canceled",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                PageNumber = 1,
                PageSize = 1 // Chỉ cần đếm, không cần lấy data
            };
            var canceledMonthData = await _bookingService.GetBookingListAsync(canceledMonthFilter);
            ViewBag.MonthCanceled = canceledMonthData?.TotalCount ?? 0;

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
            Console.WriteLine($"[DEBUG] Details called with BookingId: {id}");
            
            try
            {
                var booking = await _bookingService.GetBookingDetailAsync(id);
                Console.WriteLine($"[DEBUG] Booking retrieved: {(booking != null ? "SUCCESS" : "NULL")}");

                if (booking == null)
                {
                    Console.WriteLine($"[ERROR] Booking not found for ID: {id}");
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"[DEBUG] Booking details: {booking.BookingCode}, Status: {booking.Status}");

                // Lấy danh sách staff để hiển thị trong dropdown (nếu cần assign)
                var providerIdStr = HttpContext.Session.GetString("ProviderId");
                var token = HttpContext.Session.GetString("JWTToken");
                
                if (!string.IsNullOrEmpty(providerIdStr) && !string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"[DEBUG] Fetching staff list for ProviderId: {providerIdStr}");
                    var staffList = await _staffService.GetStaffByProviderAsync(providerIdStr, token);
                    
                    if (staffList != null)
                    {
                        ViewBag.StaffList = staffList;
                        Console.WriteLine($"[DEBUG] Loaded {staffList.Count} staff members");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to fetch staff list");
                        ViewBag.StaffList = new List<StaffDTO>();
                    }
                }
                else
                {
                    ViewBag.StaffList = new List<StaffDTO>();
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in Details: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Lỗi khi tải chi tiết đơn hàng: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Provider/Order/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateBookingStatusRequest request)
        {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine($"[FRONTEND UpdateStatus] BookingId: {request.BookingId}");
            Console.WriteLine($"[FRONTEND UpdateStatus] NewStatus: {request.NewStatus}");
            Console.WriteLine($"[FRONTEND UpdateStatus] Reason: {request.Reason ?? "NULL"}");
            Console.WriteLine($"[FRONTEND UpdateStatus] Reason Length: {request.Reason?.Length ?? 0}");
            Console.WriteLine("═══════════════════════════════════════════════");
            
            try
            {
                var dto = new UpdateBookingStatusDTO
                {
                    BookingId = request.BookingId,
                    NewStatus = request.NewStatus,
                    Reason = request.Reason  // ✨ MAP LÝ DO VÀO DTO
                };
                
                Console.WriteLine($"[FRONTEND UpdateStatus] DTO created with Reason: {dto.Reason ?? "NULL"}");
                
                var success = await _bookingService.UpdateBookingStatusAsync(dto);
                
                if (success)
                {
                    Console.WriteLine($"[FRONTEND UpdateStatus] ✅ SUCCESS");
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
                }

                Console.WriteLine($"[FRONTEND UpdateStatus] ❌ FAILED");
                return Json(new { success = false, message = "Không thể cập nhật trạng thái" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FRONTEND UpdateStatus] ❌ EXCEPTION: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Provider/Order/AssignStaff
        [HttpPost]
        public async Task<IActionResult> AssignStaff([FromBody] AssignStaffRequest request)
        {
            Console.WriteLine($"[DEBUG] AssignStaff called: BookingId={request.BookingId}, StaffId={request.StaffId}");
            
            try
            {
                var dto = new AssignStaffDTO
                {
                    BookingId = request.BookingId,
                    StaffId = request.StaffId
                };
                var success = await _bookingService.AssignStaffAsync(dto);
                
                if (success)
                {
                    Console.WriteLine($"[DEBUG] AssignStaff successful");
                    return Json(new { success = true, message = "Phân công nhân viên thành công" });
                }

                Console.WriteLine($"[ERROR] AssignStaff failed");
                return Json(new { success = false, message = "Không thể phân công nhân viên" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AssignStaff exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // Request DTOs
    public class UpdateBookingStatusRequest
    {
        public Guid BookingId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }  // ✨ LÝ DO HỦY ĐƠN
    }

    public class AssignStaffRequest
    {
        public Guid BookingId { get; set; }
        public Guid StaffId { get; set; }
    }
}

