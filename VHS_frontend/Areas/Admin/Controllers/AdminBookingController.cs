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
            // ✅ Sử dụng GetStatisticsAsync để filter theo CreatedAt (đúng cho thống kê)
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddTicks(-1); // Cuối tháng
            
            // Lấy thống kê tổng thể (filter theo CreatedAt)
            var statistics = await _bookingService.GetStatisticsAsync(thisMonthStart, thisMonthEnd);
            
            // ✅ Lấy trực tiếp từ statistics (đã filter theo CreatedAt)
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
                // Parse createdAt từ string (format: yyyy-MM-ddTHH:mm:ss)
                DateTime createdAtDateTime;
                if (!DateTime.TryParse(createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out createdAtDateTime))
                {
                    // Thử parse với format ISO 8601
                    if (!DateTime.TryParseExact(createdAt, "yyyy-MM-ddTHH:mm:ss", null, System.Globalization.DateTimeStyles.None, out createdAtDateTime))
                    {
                        return Json(new { success = false, message = $"Thời gian tạo booking không hợp lệ: {createdAt}" });
                    }
                }

                // Tính toán AutoCancelMinutes mới
                // Thời gian hủy mới = Now + remainingMinutes
                // AutoCancelMinutes = (thời gian hủy mới - CreatedAt).TotalMinutes
                // Sử dụng giờ Việt Nam để đảm bảo tính toán chính xác
                var now = DateTime.Now; // Frontend đã ở giờ Việt Nam
                var newCancelTime = now.AddMinutes(remainingMinutes);
                var newAutoCancelMinutes = (int)Math.Ceiling((newCancelTime - createdAtDateTime).TotalMinutes);
                
                // Log để debug
                System.Diagnostics.Debug.WriteLine($"UpdateCancelTime - BookingId: {bookingId}, CreatedAt: {createdAtDateTime}, Now: {now}, RemainingMinutes: {remainingMinutes}, NewAutoCancelMinutes: {newAutoCancelMinutes}");

                if (newAutoCancelMinutes < 1)
                {
                    return Json(new { success = false, message = "Không thể đặt thời gian hủy trong quá khứ." });
                }

                // Gọi API backend để cập nhật
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                httpClient.BaseAddress = new Uri("http://localhost:5154"); // Thay đổi nếu cần
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var requestBody = new { minutes = newAutoCancelMinutes };
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var apiUrl = $"/api/AdminSettings/booking/{bookingId}/auto-cancel-minutes";
                System.Diagnostics.Debug.WriteLine($"Calling API: {apiUrl}, Body: {json}");
                
                var response = await httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"API Response Status: {response.StatusCode}, Content: {responseContent}");

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

