using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Areas.Provider.Models.Staff;
using VHS_frontend.Services.Provider;
using TrackingEventDTO = VHS_frontend.Areas.Provider.Models.Booking.TrackingEventDTO;
using MediaProofDTO = VHS_frontend.Areas.Provider.Models.Booking.MediaProofDTO;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class OrderController : Controller
    {
        private readonly BookingProviderService _bookingService;
        private readonly StaffManagementService _staffService;
        private readonly ProviderSettingsService _settingsService;

        public OrderController(
            BookingProviderService bookingService,
            StaffManagementService staffService,
            ProviderSettingsService settingsService)
        {
            _bookingService = bookingService;
            _staffService = staffService;
            _settingsService = settingsService;
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
        [ResponseCache(NoStore = true, Duration = 0)] // Đảm bảo không cache
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

            // Normalize status để đảm bảo backend nhận diện đúng
            string? normalizedStatus = null;
            if (!string.IsNullOrEmpty(status))
            {
                var statusLower = status.Trim().ToLower();
                // Map "InProgress" thành các format có thể backend nhận diện
                // Backend có thể dùng "InProgress", "In Progress", hoặc "In-Progress"
                if (statusLower == "inprogress" || statusLower == "in-progress" || statusLower == "in progress")
                {
                    // Thử format "In Progress" (có khoảng trắng) vì nhiều backend dùng format này
                    normalizedStatus = "In Progress";
                }
                else
                {
                    normalizedStatus = status;
                }
            }
            
            var filter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = normalizedStatus,
                FromDate = fromDate,
                ToDate = toDate,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            Console.WriteLine($"[DEBUG] Calling API with ProviderId: {providerId}, Status: {normalizedStatus ?? "NULL"} (original: {status ?? "NULL"})");
            
            // LẤY TẤT CẢ ĐƠN HÀNG ACTIVE (không filter) để đếm Pending và Confirmed
            var allBookingsFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = null,  // Lấy tất cả active
                FromDate = null,
                ToDate = null,
                SearchTerm = null,
                PageNumber = 1,
                PageSize = 1000  // Lấy nhiều để đếm đúng
            };
            var allBookingsData = await _bookingService.GetBookingListAsync(allBookingsFilter);
            var allBookings = allBookingsData?.Items ?? new List<BookingListItemDTO>();
            
            // ĐẾM PENDING VÀ CONFIRMED từ danh sách active
            ViewBag.MonthPending = allBookings.Count(b => 
                b.Status?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true ||
                b.Status?.Contains("chờ", StringComparison.OrdinalIgnoreCase) == true ||
                b.Status?.Contains("Dang ch", StringComparison.OrdinalIgnoreCase) == true);
            
            ViewBag.MonthConfirmed = allBookings.Count(b => 
                b.Status?.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) == true ||
                b.Status?.Contains("xác nhận", StringComparison.OrdinalIgnoreCase) == true);
           
            // ĐẾM COMPLETED VÀ CANCELED - gọi API riêng vì có thể có IsDeleted=true
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1);
            
            // Đếm Completed (history status - lấy cả IsDeleted=true)
            var completedFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Completed",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                SearchTerm = null,
                PageNumber = 1,
                PageSize = 1000
            };
            var completedData = await _bookingService.GetBookingListAsync(completedFilter);
            ViewBag.MonthCompleted = completedData?.TotalCount ?? 0;
            
            // Đếm Canceled (history status - lấy cả IsDeleted=true)
            var canceledFilter = new BookingFilterDTO
            {
                ProviderId = providerId,
                Status = "Canceled",
                FromDate = thisMonthStart,
                ToDate = thisMonthEnd,
                SearchTerm = null,
                PageNumber = 1,
                PageSize = 1000
            };
            var canceledData = await _bookingService.GetBookingListAsync(canceledFilter);
            ViewBag.MonthCanceled = canceledData?.TotalCount ?? 0;
            
            Console.WriteLine($"[DEBUG] Statistics - Pending: {ViewBag.MonthPending}, Confirmed: {ViewBag.MonthConfirmed}, Completed: {ViewBag.MonthCompleted}, Canceled: {ViewBag.MonthCanceled}");

            // LẤY DANH SÁCH ĐƠN HÀNG - với filter từ user
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
            
            // NẾU KHÔNG CÓ FILTER STATUS, THÊM ĐƠN CANCELED VÀO DANH SÁCH (nếu có)
            // Vì đơn Canceled có thể có IsDeleted=true nên không hiện trong danh sách chính
            if (string.IsNullOrEmpty(status))
            {
                // Lấy thêm đơn Canceled trong tháng này để hiển thị
                var canceledForListFilter = new BookingFilterDTO
                {
                    ProviderId = providerId,
                    Status = "Canceled",
                    FromDate = thisMonthStart,
                    ToDate = thisMonthEnd,
                    SearchTerm = null,
                    PageNumber = 1,
                    PageSize = 10  // Lấy một số đơn Canceled gần đây
                };
                var canceledForList = await _bookingService.GetBookingListAsync(canceledForListFilter);
                
                if (canceledForList?.Items != null && canceledForList.Items.Any())
                {
                    // Merge vào danh sách, tránh trùng lặp
                    var existingIds = result.Items.Select(b => b.BookingId).ToHashSet();
                    var newCanceledItems = canceledForList.Items
                        .Where(b => !existingIds.Contains(b.BookingId))
                        .ToList();
                    
                    if (newCanceledItems.Any())
                    {
                        result.Items.AddRange(newCanceledItems);
                        result.TotalCount = result.Items.Count;
                        // Sắp xếp lại theo CreatedAt DESC
                        result.Items = result.Items.OrderByDescending(b => b.CreatedAt).ToList();
                        Console.WriteLine($"[DEBUG] Added {newCanceledItems.Count} canceled bookings to list");
                    }
                }
            }

            // Lấy thời gian hủy mặc định từ system settings
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                _settingsService.SetBearerToken(token);
                var defaultCancelMinutes = await _settingsService.GetDefaultAutoCancelMinutesAsync();
                ViewBag.DefaultCancelMinutes = defaultCancelMinutes;
            }
            else
            {
                ViewBag.DefaultCancelMinutes = 30; // Mặc định nếu không có token
            }

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

            // GỌI API ĐÚNG: Lấy riêng Completed và Canceled
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

            // THỐNG KÊ THÁNG NÀY CHO LỊCH SỬ - Gọi API riêng cho từng status
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
        [ResponseCache(NoStore = true, Duration = 0)] // Đảm bảo không cache để đồng bộ với admin
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
                Console.WriteLine($"[DEBUG] Timeline count from API: {booking.Timeline?.Count ?? 0}");
                Console.WriteLine($"[DEBUG] CheckerRecords count: {booking.CheckerRecords?.Count ?? 0}");

                // Timeline đã được backend tạo sẵn trong ProviderBookingService.GetBookingDetailAsync
                // Không cần tạo lại ở đây nữa
                if (booking.Timeline == null)
                {
                    booking.Timeline = new List<TrackingEventDTO>();
                }
                
                // ✨ Fallback: Nếu timeline rỗng (không nên xảy ra), tạo timeline cơ bản
                if (!booking.Timeline.Any())
                {
                    Console.WriteLine($"[DEBUG] Populating Timeline from booking data");
                    booking.Timeline = new List<TrackingEventDTO>();
                    
                    // 1. CREATED event - Luôn có
                    booking.Timeline.Add(new TrackingEventDTO
                    {
                        Time = new DateTimeOffset(booking.CreatedAt, TimeSpan.FromHours(7)),
                        Code = "CREATED",
                        Title = "Đơn hàng được tạo",
                        Description = $"Đơn hàng {booking.BookingCode} đã được tạo"
                    });
                    
                    // 2. CONFIRMED event (nếu có ConfirmedAt)
                    if (booking.ConfirmedAt.HasValue)
                    {
                        booking.Timeline.Add(new TrackingEventDTO
                        {
                            Time = new DateTimeOffset(booking.ConfirmedAt.Value, TimeSpan.FromHours(7)),
                            Code = "CONFIRMED",
                            Title = "Đơn hàng đã được xác nhận",
                            Description = booking.StaffName != null ? $"Đơn hàng đã được xác nhận bởi nhà cung cấp và nhân viên {booking.StaffName} đã được giao" : "Đơn hàng đã được xác nhận"
                        });
                    }
                    
                    // 3. CheckerRecords -> CHECK IN / CHECK OUT (nếu có)
                    if (booking.CheckerRecords != null && booking.CheckerRecords.Any())
                    {
                        Console.WriteLine($"[DEBUG] Processing {booking.CheckerRecords.Count} CheckerRecords");
                        foreach (var checker in booking.CheckerRecords.OrderBy(c => c.UploadedAt))
                        {
                            var forStatus = checker.ForStatus?.Trim().ToUpperInvariant() ?? "";
                            Console.WriteLine($"[DEBUG] CheckerRecord ForStatus: '{forStatus}', UploadedAt: {checker.UploadedAt}");
                            
                            string code, title;
                            
                            // Normalize: thay thế underscore và các ký tự đặc biệt
                            var normalized = forStatus.Replace("_", " ").Replace("-", " ").Trim();
                            
                            // CHECK OUT phải được check TRƯỚC CHECK IN để tránh match nhầm
                            // Vì "CHECKOUT" chứa cả "CHECK" và "OUT", nếu check CHECK IN trước sẽ match nhầm
                            if (normalized == "CHECKOUT" || normalized == "CHECK OUT" ||
                                normalized.StartsWith("CHECKOUT") || normalized.StartsWith("CHECK OUT") ||
                                (normalized.Contains("CHECK") && normalized.Contains("OUT") && !normalized.Contains("IN")))
                            {
                                code = "CHECK OUT";
                                title = "Check Out";
                            }
                            // CHECK IN: check sau CHECK OUT
                            else if (normalized == "CHECKIN" || normalized == "CHECK IN" ||
                                     normalized.StartsWith("CHECKIN") || normalized.StartsWith("CHECK IN") ||
                                     (normalized.Contains("CHECK") && normalized.Contains("IN") && !normalized.Contains("OUT")))
                            {
                                code = "CHECK IN";
                                title = "Check In";
                            }
                            // ✅ Các status khác
                            else if (normalized.Contains("INPROGRESS") || normalized.Contains("IN PROGRESS"))
                            {
                                code = "INPROGRESS";
                                title = "Bắt đầu làm việc";
                            }
                            else if (normalized.Contains("COMPLETED") || normalized.Contains("SERVICE COMPLETED"))
                            {
                                code = "COMPLETED";
                                title = "Hoàn thành dịch vụ";
                            }
                            else
                            {
                                // Fallback: giữ nguyên ForStatus và thử detect lại
                                code = forStatus;
                                title = $"Cập nhật: {forStatus}";
                                
                                // Nếu vẫn chưa match, log để debug
                                Console.WriteLine($"[DEBUG] WARNING: Unmatched ForStatus: '{forStatus}' -> using as-is");
                            }
                            
                            Console.WriteLine($"[DEBUG] Mapped to Code: '{code}', Title: '{title}'");
                            
                            var proofs = new List<MediaProofDTO>();
                            if (!string.IsNullOrEmpty(checker.FileUrl))
                            {
                                proofs.Add(new MediaProofDTO
                                {
                                    MediaType = checker.MediaType?.ToLower() ?? "image",
                                    Url = checker.FileUrl,
                                    Caption = checker.Description
                                });
                            }
                            
                            booking.Timeline.Add(new TrackingEventDTO
                            {
                                Time = new DateTimeOffset(checker.UploadedAt, TimeSpan.FromHours(7)),
                                Code = code,
                                Title = title,
                                Description = checker.Description,
                                Proofs = proofs
                            });
                        }
                    }
                    
                    // 3.5. Nếu có CHECK OUT nhưng chưa có CHECK IN, tạo CHECK IN event (trước CHECK OUT)
                    var hasCheckOut = booking.Timeline.Any(t => t.Code == "CHECK OUT");
                    var hasCheckIn = booking.Timeline.Any(t => t.Code == "CHECK IN");
                    if (hasCheckOut && !hasCheckIn)
                    {
                        // Tìm CHECK OUT event để lấy thời gian
                        var checkOutEvent = booking.Timeline.FirstOrDefault(t => t.Code == "CHECK OUT");
                        if (checkOutEvent != null)
                        {
                            // CHECK IN thường xảy ra trước CHECK OUT (ví dụ: 30 phút trước)
                            var checkInTime = checkOutEvent.Time.AddMinutes(-30);
                            
                            // Đảm bảo CHECK IN không sớm hơn CONFIRMED hoặc CREATED
                            var earliestTime = booking.ConfirmedAt ?? booking.CreatedAt;
                            if (checkInTime < new DateTimeOffset(earliestTime, TimeSpan.FromHours(7)))
                            {
                                checkInTime = new DateTimeOffset(earliestTime, TimeSpan.FromHours(7));
                            }
                            
                            Console.WriteLine($"[DEBUG] Creating CHECK IN event (inferred from CHECK OUT) at {checkInTime}");
                            
                            booking.Timeline.Add(new TrackingEventDTO
                            {
                                Time = checkInTime,
                                Code = "CHECK IN",
                                Title = "Check In",
                                Description = booking.StaffName != null ? $"Nhân viên {booking.StaffName} đã check in" : "Đã check in",
                                Proofs = new List<MediaProofDTO>()
                            });
                        }
                    }
                    
                    // 4. INPROGRESS event (nếu status là InProgress và chưa có)
                    var statusUpper = booking.Status?.Trim().ToUpperInvariant() ?? "";
                    if ((statusUpper.Contains("INPROGRESS") || statusUpper.Contains("IN PROGRESS")) && 
                        !booking.Timeline.Any(t => t.Code == "INPROGRESS"))
                    {
                        var inProgressTime = booking.ConfirmedAt ?? booking.CreatedAt;
                        booking.Timeline.Add(new TrackingEventDTO
                        {
                            Time = new DateTimeOffset(inProgressTime, TimeSpan.FromHours(7)),
                            Code = "INPROGRESS",
                            Title = "Bắt đầu làm việc",
                            Description = booking.StaffName != null ? $"Nhân viên {booking.StaffName} đã bắt đầu làm việc" : "Đã bắt đầu làm việc"
                        });
                    }
                    
                    // 5. COMPLETED event (nếu status là Completed và chưa có)
                    if (statusUpper.Contains("COMPLETED") && !booking.Timeline.Any(t => t.Code == "COMPLETED"))
                    {
                        var completedTime = booking.CheckerRecords?
                            .Where(c => c.ForStatus?.Contains("COMPLETED") == true || c.ForStatus?.Contains("CHECK OUT") == true)
                            .OrderByDescending(c => c.UploadedAt)
                            .FirstOrDefault()?.UploadedAt 
                            ?? booking.PaymentDate 
                            ?? booking.ConfirmedAt 
                            ?? booking.CreatedAt;
                            
                        booking.Timeline.Add(new TrackingEventDTO
                        {
                            Time = new DateTimeOffset(completedTime, TimeSpan.FromHours(7)),
                            Code = "COMPLETED",
                            Title = "Hoàn thành",
                            Description = booking.StaffName != null ? $"Đơn hàng đã được nhân viên {booking.StaffName} hoàn thành" : "Đơn hàng đã hoàn thành"
                        });
                    }
                    
                    // 6. PAYMENT event (nếu có PaymentDate)
                    if (booking.PaymentDate.HasValue && !booking.Timeline.Any(t => t.Code == "PAYMENT"))
                    {
                        booking.Timeline.Add(new TrackingEventDTO
                        {
                            Time = new DateTimeOffset(booking.PaymentDate.Value, TimeSpan.FromHours(7)),
                            Code = "PAYMENT",
                            Title = "Đã thanh toán",
                            Description = $"Đã thanh toán {booking.TotalAmount:N0} VND bằng {booking.PaymentMethod ?? "VNPAY"}"
                        });
                    }
                    
                    // 7. Sort timeline theo thời gian (tăng dần: cũ nhất -> mới nhất)
                    booking.Timeline = booking.Timeline.OrderBy(t => t.Time).ToList();
                    
                    Console.WriteLine($"[DEBUG] Populated Timeline with {booking.Timeline.Count} events");
                    foreach (var evt in booking.Timeline.OrderBy(t => t.Time))
                    {
                        Console.WriteLine($"[DEBUG] Timeline event: {evt.Code} - {evt.Title} at {evt.Time:yyyy-MM-dd HH:mm}");
                    }
                }

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

                // Lấy thời gian hủy mặc định từ system settings
                if (!string.IsNullOrEmpty(token))
                {
                    _settingsService.SetBearerToken(token);
                    var defaultCancelMinutes = await _settingsService.GetDefaultAutoCancelMinutesAsync();
                    ViewBag.DefaultCancelMinutes = defaultCancelMinutes;
                }
                else
                {
                    ViewBag.DefaultCancelMinutes = 30; // Mặc định nếu không có token
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
                    Reason = request.Reason,  // ✨ MAP LÝ DO VÀO DTO
                    SelectedStaffId = request.SelectedStaffId  // ✨ MAP SelectedStaffId VÀO DTO
                };
                
                Console.WriteLine($"[FRONTEND UpdateStatus] SelectedStaffId: {dto.SelectedStaffId?.ToString() ?? "NULL"}");
                
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

        // POST: Provider/Order/AutoCancel
        [HttpPost]
        public async Task<IActionResult> AutoCancel([FromBody] AutoCancelRequest request)
        {
            try
            {
                var success = await _bookingService.AutoCancelBookingAsync(request.BookingId, request.IsPendingExpired);
                
                if (success)
                {
                    return Json(new { success = true, message = "Đơn hàng đã được hủy tự động" });
                }
                return Json(new { success = false, message = "Không thể hủy đơn hàng" });
            }
            catch (Exception ex)
            {
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
                // Kiểm tra trạng thái booking trước khi phân công
                var booking = await _bookingService.GetBookingDetailAsync(request.BookingId);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }
                
                var status = booking.Status?.Trim() ?? string.Empty;
                if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "Canceled", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = $"Không thể phân công nhân viên cho đơn hàng có trạng thái '{booking.Status}'" });
                }
                
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
        public string? Reason { get; set; }  //  LÝ DO HỦY ĐƠN
        public Guid? SelectedStaffId { get; set; }  //  StaffId đã chọn trước khi xác nhận
    }

    public class AssignStaffRequest
    {
        public Guid BookingId { get; set; }
        public Guid StaffId { get; set; }
    }

    public class AutoCancelRequest
    {
        public Guid BookingId { get; set; }
        public bool IsPendingExpired { get; set; }
    }
}

