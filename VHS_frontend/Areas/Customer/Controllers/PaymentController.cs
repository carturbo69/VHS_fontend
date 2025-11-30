using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Services.Customer;
using VHS_frontend.Services.Customer.Interfaces;
using VHS_frontend.Models.Payment;
using Microsoft.AspNetCore.Http;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class PaymentController : Controller
    {
        private readonly BookingServiceCustomer _bookingServiceCustomer;
        private readonly IVnPayService _vnPayService;

        public PaymentController(BookingServiceCustomer bookingServiceCustomer, IVnPayService vnPayService)
        {
            _bookingServiceCustomer = bookingServiceCustomer;
            _vnPayService = vnPayService;
        }

        // ====== Helpers chung ======
        private bool NotLoggedIn()
        {
            var jwt = HttpContext.Session.GetString("JWToken");
            var acc = HttpContext.Session.GetString("AccountID");
            return string.IsNullOrWhiteSpace(jwt) || string.IsNullOrWhiteSpace(acc);
        }

        private Dictionary<Guid, string> GetServiceNamesFromSession()
        {
            var serviceNames = new Dictionary<Guid, string>();
            var serviceNamesJson = HttpContext.Session.GetString("BookingServiceNamesJson");
            if (!string.IsNullOrWhiteSpace(serviceNamesJson))
            {
                try
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(serviceNamesJson);
                    if (map != null)
                    {
                        foreach (var kvp in map)
                        {
                            if (Guid.TryParse(kvp.Key, out var bookingId))
                            {
                                serviceNames[bookingId] = kvp.Value;
                            }
                        }
                    }
                }
                catch { /* bỏ qua */ }
            }
            return serviceNames;
        }

        /// <summary>
        /// Lấy tên dịch vụ từ API nếu session không có (dùng cho trường hợp mất session sau thanh toán)
        /// </summary>
        private async Task<Dictionary<Guid, string>> GetServiceNamesFromApiAsync(List<Guid> bookingIds, CancellationToken ct = default)
        {
            var serviceNames = new Dictionary<Guid, string>();
            
            if (bookingIds == null || !bookingIds.Any())
                return serviceNames;

            var jwt = HttpContext.Session.GetString("JWToken");
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            
            // Nếu không có token hoặc accountId, không thể gọi API
            if (string.IsNullOrWhiteSpace(jwt) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                System.Diagnostics.Debug.WriteLine("[Payment] Không có token hoặc accountId, không thể lấy service names từ API");
                return serviceNames;
            }

            try
            {
                // Lấy tên dịch vụ từ API cho mỗi booking
                foreach (var bookingId in bookingIds)
                {
                    try
                    {
                        var bookingDetail = await _bookingServiceCustomer.GetHistoryDetailAsync(accountId, bookingId, jwt, ct);
                        if (bookingDetail != null && bookingDetail.Service != null && !string.IsNullOrWhiteSpace(bookingDetail.Service.Title))
                        {
                            serviceNames[bookingId] = bookingDetail.Service.Title;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Payment] Lỗi khi lấy booking detail cho {bookingId}: {ex.Message}");
                        // Tiếp tục với booking khác
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Payment] Lỗi khi lấy service names từ API: {ex.Message}");
            }

            return serviceNames;
        }

        /// <summary>
        /// Đọc Session "BookingBreakdownJson" để tính tổng Amount cho các bookingIds được chọn.
        /// "BookingBreakdownJson" đã được set ở PlaceOrder.
        /// </summary>
        private decimal ComputeAmountFromSession(List<Guid> bookingIds)
        {
            var json = HttpContext.Session.GetString("BookingBreakdownJson");
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("Không tìm thấy breakdown trong phiên làm việc. Vui lòng đặt hàng lại.");
            }

            var breakdown = JsonSerializer.Deserialize<List<BookingAmountItem>>(json) ?? new List<BookingAmountItem>();

            var total = breakdown
                .Where(b => bookingIds.Contains(b.BookingId))
                .Sum(b => b.Amount);

            return Math.Max(0m, total);
        }

        private class BookingAmountItem
        {
            public Guid BookingId { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Discount { get; set; }
            public decimal Amount { get; set; }
        }

        private async Task CancelPendingFromIdsOrSessionAsync(List<Guid>? bookingIds, CancellationToken ct)
        {
            var jwt = HttpContext.Session.GetString("JWToken");
            var didCall = false;

            try
            {
                if (bookingIds?.Any() == true)
                {
                    didCall = true;
                    await _bookingServiceCustomer.CancelUnpaidAsync(bookingIds, jwt, ct);
                }
                else
                {
                    var csv = HttpContext.Session.GetString("CHECKOUT_PENDING_BOOKING_IDS");
                    if (!string.IsNullOrWhiteSpace(csv))
                    {
                        var pendingIds = csv
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                            .Where(g => g != Guid.Empty)
                            .Distinct()
                            .ToList();

                        if (pendingIds.Count > 0)
                        {
                            didCall = true;
                            await _bookingServiceCustomer.CancelUnpaidAsync(pendingIds, jwt, ct);
                        }
                    }
                }

                // Chỉ xoá session khi API hủy thành công
                if (didCall)
                    HttpContext.Session.Remove("CHECKOUT_PENDING_BOOKING_IDS");
            }
            catch
            {
                TempData["ToastError"] = "Không thể hủy đơn đang chờ thanh toán (vui lòng thử lại).";
                // Không xoá session pending để user có thể thử hủy lại.
            }
        }

        // ====== SUCCESS PAGE ======

        public class PaymentSuccessViewModel
        {
            public string Gateway { get; set; } = string.Empty; // VNPAY / MOMO
            public List<Guid> BookingIds { get; set; } = new();
            public Dictionary<Guid, string> ServiceNames { get; set; } = new(); // BookingId -> ServiceName
            public decimal Total { get; set; }
            public string? Note { get; set; }
        }

        [HttpGet]
        public IActionResult Success([FromQuery] List<Guid>? bookingIds, [FromQuery] decimal? total, [FromQuery] string? gateway)
        {
            // (Success có thể không bắt login; nếu muốn bắt, thêm NotLoggedIn guard như các action khác)
            var ids = bookingIds ?? new List<Guid>();
            var amt = total ?? 0m;

            // Fallback: nếu thiếu total nhưng có ids -> thử tính lại từ Session
            if ((!total.HasValue || total.Value <= 0) && ids.Any())
            {
                try { amt = ComputeAmountFromSession(ids); }
                catch { /* bỏ qua, giữ 0 */ }
            }

            var vm = new PaymentSuccessViewModel
            {
                Gateway = string.IsNullOrWhiteSpace(gateway) ? "Cổng thanh toán" : gateway!,
                BookingIds = ids,
                ServiceNames = GetServiceNamesFromSession(),
                Total = amt,
                Note = TempData["ToastSuccess"] as string
            };

            return View("Success", vm);
        }

        // HỦY rồi về Giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelToCart(CancellationToken ct)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            await CancelPendingFromIdsOrSessionAsync(null, ct);
            return RedirectToAction("Index", "Cart", new { area = "Customer" });
        }

        // HỦY rồi về trang Index (BookingService)
        [HttpPost] // nếu muốn giữ GET cũng được, nhưng POST an toàn hơn
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAndBack(CancellationToken ct)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            await CancelPendingFromIdsOrSessionAsync(null, ct);
            return RedirectToAction("Index", "BookingService", new { area = "Customer", refresh = 1 });
        }

        // ====== VNPay Integration - Standard Methods ======

        /// <summary>
        /// Bắt đầu thanh toán VNPay từ booking
        /// URL: /Customer/Payment/StartVnPay?bookingIds=guid1,guid2&amount=810000.00
        /// </summary>
        [HttpGet]
        public IActionResult StartVnPay([FromQuery] List<Guid>? bookingIds, [FromQuery] string? amount)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Nếu bookingIds rỗng, thử parse từ query string dạng comma-separated
            if (bookingIds == null || bookingIds.Count == 0)
            {
                var bookingIdsStr = Request.Query["bookingIds"].ToString();
                if (!string.IsNullOrWhiteSpace(bookingIdsStr))
                {
                    bookingIds = bookingIdsStr
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                        .Where(g => g != Guid.Empty)
                        .Distinct()
                        .ToList();
                }
            }

            if (bookingIds == null || bookingIds.Count == 0)
            {
                TempData["ToastError"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }

            // Parse amount từ string (dùng InvariantCulture)
            decimal amountDecimal = 0m;
            if (!decimal.TryParse(amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amountDecimal))
            {
                // Fallback: thử tính từ session nếu amount không hợp lệ
                try
                {
                    amountDecimal = ComputeAmountFromSession(bookingIds);
                }
                catch
                {
                    TempData["ToastError"] = "Số tiền thanh toán không hợp lệ. Vui lòng thử lại.";
                    return RedirectToAction("Index", "BookingService", new { area = "Customer" });
                }
            }


            // Lưu booking IDs vào session để callback có thể sử dụng
            HttpContext.Session.SetString("CHECKOUT_PENDING_BOOKING_IDS", string.Join(",", bookingIds));

            // Lưu service names vào session cho success page
            var serviceNames = GetServiceNamesFromSession();
            foreach (var bookingId in bookingIds)
            {
                if (!serviceNames.ContainsKey(bookingId))
                {
                    // Nếu chưa có trong session, có thể lấy từ API hoặc để trống
                    // serviceNames sẽ được cập nhật sau nếu cần
                }
            }

            // Build OrderDescription theo format: "BOOKINGS:guid1,guid2,guid3"
            var orderDescription = $"BOOKINGS:{string.Join(",", bookingIds)}";

            // Tạo PaymentInformationModel
            var paymentModel = new PaymentInformationModel
            {
                OrderType = "other",
                Amount = (double)amountDecimal,
                OrderDescription = orderDescription,
                Name = "Thanh toán dịch vụ"
            };

            // Tạo URL thanh toán và redirect
            try
            {
                var url = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
                return Redirect(url);
            }
            catch (ArgumentException ex)
            {
                // Xử lý lỗi validation từ VnPayService
                TempData["ToastError"] = ex.Message;
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                System.Diagnostics.Debug.WriteLine($"[Payment] Lỗi khi tạo URL thanh toán VNPay: {ex.Message}");
                TempData["ToastError"] = "Có lỗi xảy ra khi tạo liên kết thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }
        }

        /// <summary>
        /// Tạo URL thanh toán VNPay và chuyển hướng người dùng đến cổng thanh toán
        /// </summary>
        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            try
            {
                var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
                return Redirect(url);
            }
            catch (ArgumentException ex)
            {
                // Xử lý lỗi validation từ VnPayService
                TempData["ToastError"] = ex.Message;
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                System.Diagnostics.Debug.WriteLine($"[Payment] Lỗi khi tạo URL thanh toán VNPay: {ex.Message}");
                TempData["ToastError"] = "Có lỗi xảy ra khi tạo liên kết thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }
        }

        /// <summary>
        /// Callback từ VNPay sau khi thanh toán (trả về JSON - dùng cho API)
        /// </summary>
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            return Json(response);
        }

        /// <summary>
        /// Confirm VNPay payment sau khi user login lại (do mất session khi redirect qua ngrok)
        /// URL: /Customer/Payment/ConfirmVnPayAfterLogin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ConfirmVnPayAfterLogin(string? bookingIds, string? transactionId, CancellationToken ct = default)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(bookingIds))
            {
                TempData["ToastError"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }

            var bookingIdList = bookingIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();

            if (bookingIdList.Count == 0)
            {
                TempData["ToastError"] = "Danh sách booking không hợp lệ.";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }

            try
            {
                var jwt = HttpContext.Session.GetString("JWToken");

                // Lấy thời gian hiện tại theo timezone Việt Nam
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var paymentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                
                // Confirm payment với backend
                await _bookingServiceCustomer.ConfirmPaymentsAsync(
                    new ConfirmPaymentsDto
                    {
                        BookingIds = bookingIdList,
                        PaymentMethod = "VNPAY",
                        GatewayTxnId = $"VNPAY:{transactionId ?? "UNKNOWN"}",
                        CartItemIdsForCleanup = null, // Không cleanup cart vì đã mất session
                        PaymentTime = paymentTime // Gửi thời gian chính xác
                    },
                    jwt,
                    ct);

                // Tính total từ session
                var totalAfterLogin = 0m;
                try
                {
                    totalAfterLogin = ComputeAmountFromSession(bookingIdList);
                }
                catch
                {
                    // Nếu không lấy được từ session, để mặc định 0
                    System.Diagnostics.Debug.WriteLine("[VNPay Warning] Không lấy được amount từ session trong ConfirmVnPayAfterLogin.");
                }
                
                // Lấy tên dịch vụ - ưu tiên session, nếu không có thì lấy từ API
                var serviceNamesAfterLogin = GetServiceNamesFromSession();
                if (!serviceNamesAfterLogin.Any())
                {
                    // Nếu session không có, lấy từ API
                    serviceNamesAfterLogin = await GetServiceNamesFromApiAsync(bookingIdList, ct);
                }

                TempData["ToastSuccess"] = $"Thanh toán VNPay thành công! Mã giao dịch: {transactionId}";
                
                // Hiển thị trang success đẹp
                ViewBag.TransactionId = transactionId;
                ViewBag.BookingIds = bookingIdList;
                ViewBag.ServiceNames = serviceNamesAfterLogin;
                ViewBag.NeedLogin = false;
                ViewBag.Total = totalAfterLogin;
                
                return View("VnPaySuccess");
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = "Có lỗi khi xác nhận thanh toán: " + ex.Message;
                return RedirectToAction("ListHistoryBooking", "BookingService", new { area = "Customer" });
            }
        }

        /// <summary>
        /// Return URL từ VNPay sau khi người dùng thanh toán (xử lý và hiển thị kết quả)
        /// URL: /Customer/Payment/VnPayReturnUrl
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayReturnUrl(CancellationToken ct = default)
        {
            try
            {
                // Parse và validate response từ VNPay
                var response = _vnPayService.PaymentExecute(Request.Query);

                if (!response.Success)
                {
                    // Thanh toán thất bại - xác định lý do cụ thể
                    string errorMessage = response.VnPayResponseCode switch
                    {
                        "24" => "Giao dịch bị hủy bởi người dùng",
                        "07" => "Trừ tiền thành công nhưng giao dịch bị nghi ngờ (liên quan đến fraud, giao dịch bất thường)",
                        "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking tại ngân hàng",
                        "10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                        "11" => "Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch",
                        "12" => "Thẻ/Tài khoản bị khóa",
                        "13" => "Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch",
                        "51" => "Tài khoản không đủ số dư để thực hiện giao dịch",
                        "65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày",
                        "75" => "Ngân hàng thanh toán đang bảo trì",
                        "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch",
                        _ => $"Thanh toán thất bại (Mã lỗi: {response.VnPayResponseCode})"
                    };

                    TempData["ToastError"] = errorMessage;
                    
                    // LẤY BOOKING IDS ĐỂ HỦY (từ OrderDescription hoặc session)
                    List<Guid>? bookingIdsToCancel = null;
                    var orderInfoCancel = response.OrderDescription ?? "";
                    
                    if (orderInfoCancel.StartsWith("BOOKINGS:", StringComparison.OrdinalIgnoreCase))
                    {
                        var bookingIdsPart = orderInfoCancel.Substring("BOOKINGS:".Length);
                        bookingIdsToCancel = bookingIdsPart
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                            .Where(g => g != Guid.Empty)
                            .Distinct()
                            .ToList();
                    }
                    else
                    {
                        // Fallback: lấy từ session
                        var pendingBookingsCsv = HttpContext.Session.GetString("CHECKOUT_PENDING_BOOKING_IDS");
                        if (!string.IsNullOrWhiteSpace(pendingBookingsCsv))
                        {
                            bookingIdsToCancel = pendingBookingsCsv
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                .Where(g => g != Guid.Empty)
                                .Distinct()
                                .ToList();
                        }
                    }
                    
                    // HỦY BOOKING ĐANG CHỜ (truyền bookingIds cụ thể nếu có)
                    await CancelPendingFromIdsOrSessionAsync(bookingIdsToCancel, ct);
                    
                    // Nếu chưa login, hiển thị thông báo nhưng vẫn đã hủy booking
                    if (NotLoggedIn())
                    {
                        return Content($"Thanh toán thất bại. {errorMessage}. Vui lòng đăng nhập lại và thử lại.");
                    }
                    
                    return RedirectToAction("Index", "BookingService", new { area = "Customer", refresh = 1 });
                }

                // LẤY BOOKING IDS TỪ VNPAY RESPONSE TRƯỚC (không cần login)
                var jwt = HttpContext.Session.GetString("JWToken");
                
                // Parse booking IDs từ OrderDescription (format: "BOOKINGS:guid1,guid2,guid3")
                var orderInfo = response.OrderDescription ?? "";
                List<Guid> bookingIds;
                
                // DEBUG: Log để xem VNPay trả về gì
                System.Diagnostics.Debug.WriteLine($"[VNPay Debug] OrderDescription: '{orderInfo}'");
                System.Diagnostics.Debug.WriteLine($"[VNPay Debug] TransactionId: {response.TransactionId}");
                
                if (orderInfo.StartsWith("BOOKINGS:", StringComparison.OrdinalIgnoreCase))
                {
                    var bookingIdsPart = orderInfo.Substring("BOOKINGS:".Length);
                    bookingIds = bookingIdsPart
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                        .Where(g => g != Guid.Empty)
                        .Distinct()
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[VNPay Debug] Parsed {bookingIds.Count} booking IDs from OrderDescription");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[VNPay Debug] OrderDescription không có format 'BOOKINGS:', thử lấy từ session");
                    
                    // Fallback: thử lấy từ session (cho backward compatibility)
                    var pendingBookingsCsv = HttpContext.Session.GetString("CHECKOUT_PENDING_BOOKING_IDS");
                    
                    System.Diagnostics.Debug.WriteLine($"[VNPay Debug] Session CHECKOUT_PENDING_BOOKING_IDS: '{pendingBookingsCsv ?? "(null)"}'");
                    
                    if (string.IsNullOrWhiteSpace(pendingBookingsCsv))
                    {
                        // Không tìm thấy ở cả 2 nơi → Hiển thị debug info
                        var debugInfo = $@"
                        <h2>Debug Info - VNPay Callback</h2>
                        <p><strong>Thanh toán thành công!</strong></p>
                        <p>Mã giao dịch: <strong>{response.TransactionId}</strong></p>
                        <p>Response Code: <strong>{response.VnPayResponseCode}</strong></p>
                        <hr/>
                        <h3>❌ Không tìm thấy thông tin booking</h3>
                        <p>OrderDescription từ VNPay: <code>{System.Net.WebUtility.HtmlEncode(orderInfo)}</code></p>
                        <p>Session CHECKOUT_PENDING_BOOKING_IDS: <code>null hoặc empty</code></p>
                        <hr/>
                        <p>Vui lòng chụp màn hình này và kiểm tra log!</p>
                        <p><a href='/Customer/BookingService/ListHistoryBooking'>Xem lịch sử booking</a></p>
                        ";
                        
                        return Content(debugInfo, "text/html");
                    }
                    
                    bookingIds = pendingBookingsCsv
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                        .Where(g => g != Guid.Empty)
                        .Distinct()
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[VNPay Debug] Parsed {bookingIds.Count} booking IDs from session");
                }

                if (bookingIds.Count == 0)
                {
                    TempData["ToastError"] = "Danh sách booking không hợp lệ.";
                    return RedirectToAction("Index", "Cart", new { area = "Customer" });
                }

                //  Lấy tên dịch vụ - khai báo ở scope ngoài để dùng chung cho cả 2 trường hợp
                Dictionary<Guid, string> serviceNames;

                //  Kiểm tra đăng nhập NGAY TẠI ĐÂY (sau khi đã parse được booking IDs)
                if (NotLoggedIn())
                {
                    // Tính total cho trường hợp chưa đăng nhập
                    var totalBeforeLogin = 0m;
                    try
                    {
                        totalBeforeLogin = ComputeAmountFromSession(bookingIds);
                    }
                    catch
                    {
                        // Nếu không lấy được từ session, lấy từ VNPay response Amount (đã chia cho 100 rồi)
                        if (response.Amount > 0)
                        {
                            totalBeforeLogin = response.Amount;
                        }
                    }
                    
                    //  Lấy tên dịch vụ - ưu tiên session, nếu không có thì lấy từ API (nếu đã đăng nhập sau thanh toán)
                    serviceNames = GetServiceNamesFromSession();
                    if (!serviceNames.Any() && !NotLoggedIn())
                    {
                        // Nếu session không có và đã đăng nhập, lấy từ API
                        serviceNames = await GetServiceNamesFromApiAsync(bookingIds, ct);
                    }
                    
                    // Hiển thị trang success đẹp thay vì redirect login
                    ViewBag.TransactionId = response.TransactionId;
                    ViewBag.BookingIds = bookingIds;
                    ViewBag.ServiceNames = serviceNames;
                    ViewBag.NeedLogin = true;
                    ViewBag.Total = totalBeforeLogin;
                    
                    TempData["ToastSuccess"] = $"Thanh toán VNPay thành công! Mã giao dịch: {response.TransactionId}";
                    
                    return View("VnPaySuccess");
                }

                // Lấy cartItemIds để cleanup (nếu có)
                var cartCsv = HttpContext.Session.GetString("CHECKOUT_SELECTED_IDS");
                List<Guid>? cartItemIds = null;
                if (!string.IsNullOrWhiteSpace(cartCsv))
                {
                    cartItemIds = cartCsv
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                        .Where(g => g != Guid.Empty)
                        .Distinct()
                        .ToList();
                }

                // Lấy thời gian hiện tại theo timezone Việt Nam
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var paymentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                
                // Confirm payment với backend
                await _bookingServiceCustomer.ConfirmPaymentsAsync(
                    new ConfirmPaymentsDto
                    {
                        BookingIds = bookingIds,
                        PaymentMethod = "VNPAY",
                        GatewayTxnId = $"VNPAY:{response.TransactionId}",
                        CartItemIdsForCleanup = cartItemIds,
                        PaymentTime = paymentTime // ✅ Gửi thời gian chính xác
                    },
                    jwt,
                    ct);

                // Tính lại total từ breakdown
                var total = 0m;
                try
                {
                    total = ComputeAmountFromSession(bookingIds);
                }
                catch
                {
                    // Nếu không lấy được từ session, lấy từ VNPay response Amount (đã chia cho 100 rồi)
                    if (response.Amount > 0)
                    {
                        total = response.Amount;
                    }
                    else
                    {
                        // Fallback: nếu response không có Amount, tính tổng từ bookings (nếu có token)
                        // Không dùng OrderId vì nó là txnRef (timestamp ticks), không phải amount
                        System.Diagnostics.Debug.WriteLine("[VNPay Warning] Không lấy được amount từ response và session. Sử dụng giá trị mặc định 0.");
                    }
                }

                // Dọn session
                HttpContext.Session.Remove("CHECKOUT_PENDING_BOOKING_IDS");
                HttpContext.Session.Remove("CHECKOUT_DIRECT_JSON");
                HttpContext.Session.Remove("CHECKOUT_SELECTED_IDS");

                TempData["ToastSuccess"] = "Thanh toán VNPay thành công!";
                
                // Lấy tên dịch vụ - ưu tiên session, nếu không có thì lấy từ API
                serviceNames = GetServiceNamesFromSession();
                if (!serviceNames.Any())
                {
                    // Nếu session không có, lấy từ API
                    serviceNames = await GetServiceNamesFromApiAsync(bookingIds, ct);
                }
                
                // Hiển thị trang success đẹp
                ViewBag.TransactionId = response.TransactionId;
                ViewBag.BookingIds = bookingIds;
                ViewBag.ServiceNames = serviceNames;
                ViewBag.NeedLogin = false;
                ViewBag.Total = total;
                
                return View("VnPaySuccess");
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = "Có lỗi khi xử lý thanh toán: " + ex.Message;
                await CancelPendingFromIdsOrSessionAsync(null, ct);
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }
        }

    }
}
