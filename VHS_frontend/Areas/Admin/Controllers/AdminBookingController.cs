using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Booking;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Areas.Provider.Models.Staff;
using VHS_frontend.Services.Admin;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBookingController : Controller
    {
        private readonly AdminBookingService _bookingService;
        private readonly StaffManagementService _staffService;

        public AdminBookingController(AdminBookingService bookingService, StaffManagementService staffService)
        {
            _bookingService = bookingService;
            _staffService = staffService;
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

            // THỐNG KÊ THÁNG NÀY - Lấy từng loại riêng biệt
            // Sử dụng GetStatisticsAsync để filter theo CreatedAt (đúng cho thống kê)
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddTicks(-1); // Cuối tháng
            
            // Lấy thống kê tổng thể (filter theo CreatedAt)
            var statistics = await _bookingService.GetStatisticsAsync(thisMonthStart, thisMonthEnd);
            
            // Lấy trực tiếp từ statistics (đã filter theo CreatedAt)
            ViewBag.MonthPending = statistics?.PendingBookings ?? 0;
            ViewBag.MonthConfirmed = statistics?.ConfirmedBookings ?? 0;
            ViewBag.MonthCompleted = statistics?.CompletedBookings ?? 0;
            ViewBag.MonthCanceled = statistics?.CancelledBookings ?? 0;

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

                // Timeline đã được backend tạo sẵn trong AdminBookingService.GetBookingDetailAsync
                // Không cần tạo lại ở đây nữa
                if (booking.Timeline == null)
                {
                    booking.Timeline = new List<TrackingEventDTO>();
                }
                
                // ✨ Fallback: Nếu timeline rỗng (không nên xảy ra), tạo timeline cơ bản
                if (!booking.Timeline.Any())
                {
                    booking.Timeline = new List<TrackingEventDTO>();

                    // 1. CREATED event
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

                    // 3. CheckerRecords -> CHECK IN / CHECK OUT
                    if (booking.CheckerRecords != null && booking.CheckerRecords.Any())
                    {
                        foreach (var checker in booking.CheckerRecords.OrderBy(c => c.UploadedAt))
                        {
                            var forStatus = checker.ForStatus?.Trim().ToUpperInvariant() ?? "";
                            var normalized = forStatus.Replace("_", " ").Replace("-", " ").Trim();
                            
                            string code, title;
                            
                            if (normalized == "CHECKOUT" || normalized == "CHECK OUT" ||
                                normalized.StartsWith("CHECKOUT") || normalized.StartsWith("CHECK OUT") ||
                                (normalized.Contains("CHECK") && normalized.Contains("OUT") && !normalized.Contains("IN")))
                            {
                                code = "CHECK OUT";
                                title = "Check Out";
                            }
                            else if (normalized == "CHECKIN" || normalized == "CHECK IN" ||
                                     normalized.StartsWith("CHECKIN") || normalized.StartsWith("CHECK IN") ||
                                     (normalized.Contains("CHECK") && normalized.Contains("IN") && !normalized.Contains("OUT")))
                            {
                                code = "CHECK IN";
                                title = "Check In";
                            }
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
                                code = forStatus;
                                title = $"Cập nhật: {forStatus}";
                            }
                            
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

                    // 4. Nếu có CHECK OUT nhưng chưa có CHECK IN
                    var hasCheckOut = booking.Timeline.Any(t => t.Code == "CHECK OUT");
                    var hasCheckIn = booking.Timeline.Any(t => t.Code == "CHECK IN");
                    if (hasCheckOut && !hasCheckIn)
                    {
                        var checkOutEvent = booking.Timeline.FirstOrDefault(t => t.Code == "CHECK OUT");
                        if (checkOutEvent != null)
                        {
                            var checkInTime = checkOutEvent.Time.AddMinutes(-30);
                            var earliestTime = booking.ConfirmedAt ?? booking.CreatedAt;
                            if (checkInTime < new DateTimeOffset(earliestTime, TimeSpan.FromHours(7)))
                            {
                                checkInTime = new DateTimeOffset(earliestTime, TimeSpan.FromHours(7));
                            }
                            
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

                    // 5. INPROGRESS event
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

                    // 6. COMPLETED event
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

                    // 7. PAYMENT event
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

                    // Sort timeline
                    booking.Timeline = booking.Timeline.OrderBy(t => t.Time).ToList();
                }

                // Lấy danh sách staff để hiển thị ảnh trong timeline
                // Admin không có ProviderId trong session, nên sẽ lấy staff từ StaffId nếu có
                // token đã được khai báo ở đầu method
                if (booking.StaffId.HasValue && !string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var staffIdStr = booking.StaffId.Value.ToString();
                        var staff = await _staffService.GetStaffByIdAsync(staffIdStr, token);
                        
                        if (staff != null)
                        {
                            // Tạo danh sách staff với 1 phần tử để view có thể sử dụng
                            ViewBag.StaffList = new List<StaffDTO> { staff };
                        }
                        else
                        {
                            ViewBag.StaffList = new List<StaffDTO>();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Admin] Error fetching staff: {ex.Message}");
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
                TempData["ErrorMessage"] = $"Lỗi khi tải chi tiết đơn hàng: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/AdminBooking/UpdateAutoCancelMinutes
        // Admin cập nhật thời gian tự động hủy cho booking (từ CreatedAt)
        [HttpPost]
        public async Task<IActionResult> UpdateAutoCancelMinutes(Guid bookingId, int? minutes)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            }

            try
            {
                // Gọi API backend để cập nhật
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                
                var backendUrl = "http://localhost:5154"; // Có thể lấy từ config
                httpClient.BaseAddress = new Uri(backendUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var requestBody = new { Minutes = minutes };
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var apiUrl = $"/api/AdminSettings/booking/{bookingId}/auto-cancel-minutes";
                
                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                        var apiMessage = apiResult.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Đã cập nhật thành công";
                        
                        return Json(new { 
                            success = true, 
                            message = apiMessage,
                            minutes = minutes
                        });
                    }
                    catch
                    {
                        return Json(new { 
                            success = true, 
                            message = "Đã cập nhật thời gian hủy thành công!",
                            minutes = minutes
                        });
                    }
                }
                else
                {
                    try
                    {
                        var errorResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                        var errorMessage = errorResult.TryGetProperty("message", out var errProp) ? errProp.GetString() : 
                                         (errorResult.TryGetProperty("error", out var errField) ? errField.GetString() : null);
                        
                        return Json(new { 
                            success = false, 
                            message = errorMessage ?? $"Lỗi HTTP {response.StatusCode}" 
                        });
                    }
                    catch
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Lỗi khi cập nhật: HTTP {response.StatusCode}" 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/AdminBooking/UpdateCancelTime
        // Admin cập nhật thời gian hủy cho booking
        [HttpPost]
        public async Task<IActionResult> UpdateCancelTime(Guid bookingId, int remainingMinutes, string createdAt)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            }

            if (remainingMinutes < 1)
            {
                return Json(new { success = false, message = "Thời gian còn lại phải lớn hơn 0 phút." });
            }

            try
            {
                // QUAN TRỌNG: Parse createdAt từ string (có thể là CreatedAt hoặc ConfirmedAt)
                // Format: yyyy-MM-ddTHH:mm:ss hoặc yyyy-MM-ddTHH:mm:ss+07:00
                DateTime createdAtDateTime;
                
                // Thử parse với nhiều format để đảm bảo nhất quán với frontend
                if (!DateTime.TryParse(createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out createdAtDateTime))
                {
                    // Thử parse với format ISO 8601 không có timezone
                    if (!DateTime.TryParseExact(createdAt, "yyyy-MM-ddTHH:mm:ss", null, System.Globalization.DateTimeStyles.None, out createdAtDateTime))
                    {
                        // Thử parse với format có timezone +07:00
                        if (!DateTime.TryParseExact(createdAt, "yyyy-MM-ddTHH:mm:ss+07:00", null, System.Globalization.DateTimeStyles.None, out createdAtDateTime))
                        {
                            return Json(new { success = false, message = $"Thời gian booking không hợp lệ: {createdAt}" });
                        }
                    }
                }

                // QUAN TRỌNG: Tính toán AutoCancelMinutes mới
                // Với Confirmed booking: createdAtDateTime thực ra là ConfirmedAt (đã được frontend truyền đúng)
                // Với Pending booking: createdAtDateTime là CreatedAt
                // Thời gian hủy mới = Now + remainingMinutes
                // AutoCancelMinutes = (thời gian hủy mới - createdAtDateTime).TotalMinutes
                var now = DateTime.Now;
                var newCancelTime = now.AddMinutes(remainingMinutes);
                var newAutoCancelMinutes = (int)Math.Ceiling((newCancelTime - createdAtDateTime).TotalMinutes);

                if (newAutoCancelMinutes < 1)
                {
                    return Json(new { success = false, message = "Không thể đặt thời gian hủy trong quá khứ." });
                }

                // Gọi API backend để cập nhật
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                httpClient.BaseAddress = new Uri("http://localhost:5154");
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var requestBody = new { minutes = newAutoCancelMinutes };
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var apiUrl = $"/api/AdminSettings/booking/{bookingId}/auto-cancel-minutes";
                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // API trả về { message, bookingId, minutes }
                    try
                    {
                        var apiResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                        var apiMessage = apiResult.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Đã cập nhật thành công";
                        
                        return Json(new { 
                            success = true, 
                            message = $"Đã cập nhật thời gian hủy thành công! Booking sẽ bị hủy sau {remainingMinutes} phút.",
                            newAutoCancelMinutes = newAutoCancelMinutes,
                            apiResponse = apiMessage
                        });
                    }
                    catch
                    {
                        return Json(new { 
                            success = true, 
                            message = $"Đã cập nhật thời gian hủy thành công! Booking sẽ bị hủy sau {remainingMinutes} phút.",
                            newAutoCancelMinutes = newAutoCancelMinutes
                        });
                    }
                }
                else
                {
                    // Parse error response
                    try
                    {
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            return Json(new { success = false, message = $"Lỗi khi cập nhật: HTTP {response.StatusCode} - Response rỗng" });
                        }
                        
                        var errorResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                        var errorMessage = errorResult.TryGetProperty("message", out var errProp) ? errProp.GetString() : null;
                        
                        if (string.IsNullOrWhiteSpace(errorMessage))
                        {
                            // Thử lấy error field
                            errorMessage = errorResult.TryGetProperty("error", out var errField) ? errField.GetString() : null;
                        }
                        
                        if (string.IsNullOrWhiteSpace(errorMessage))
                        {
                            errorMessage = responseContent;
                        }
                        
                        return Json(new { success = false, message = $"Lỗi khi cập nhật: {errorMessage}" });
                    }
                    catch (Exception parseEx)
                    {
                        return Json(new { success = false, message = $"Lỗi khi cập nhật: HTTP {response.StatusCode} - {responseContent} (Parse error: {parseEx.Message})" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}

