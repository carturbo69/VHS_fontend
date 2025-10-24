using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Services.Customer;
using VHS_frontend.Services.Customer.Interfaces;
using VHS_frontend.Models.Payment;

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

        // ====== PAGES DEMO ======

        /// <summary>
        /// Trang giả lập gọi VNPay — nhận bookingIds qua query
        /// </summary>
        [HttpGet]
        public IActionResult StartVnPay([FromQuery] List<Guid> bookingIds)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            if (bookingIds == null || bookingIds.Count == 0)
            {
                TempData["ToastError"] = "Thiếu danh sách booking.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            decimal amount;
            try
            {
                amount = ComputeAmountFromSession(bookingIds);
            }
            catch (InvalidOperationException)
            {
                TempData["ToastError"] = "Phiên thanh toán đã hết hoặc không hợp lệ. Vui lòng đặt hàng lại.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            var orderInfo = $"Thanh toán {string.Join(",", bookingIds)}";

            var simulateSuccessUrl = Url.Action(nameof(VnPayReturn), "Payment",
                new
                {
                    area = "Customer",
                    result = "success",
                    vnp_ResponseCode = "00",
                    bookingIds = bookingIds,
                    amount = amount.ToString(CultureInfo.InvariantCulture),
                    orderInfo
                },
                Request.Scheme)!;

            var simulateFailUrl = Url.Action(nameof(VnPayReturn), "Payment",
                new
                {
                    area = "Customer",
                    result = "fail",
                    vnp_ResponseCode = "24",
                    message = "User canceled",
                    bookingIds = bookingIds,
                    amount = amount.ToString(CultureInfo.InvariantCulture),
                    orderInfo
                },
                Request.Scheme)!;

            var vm = new PaymentDemoViewModel
            {
                Gateway = "VNPay",
                BookingIds = bookingIds,
                Amount = amount,
                Message = "Đây là trang demo thanh toán VNPay.",
                SimulateSuccessUrl = simulateSuccessUrl,
                SimulateFailUrl = simulateFailUrl
            };
            return View("StartVnPay", vm);
        }

        /// <summary>
        /// Trang giả lập gọi MoMo — nhận bookingIds qua query
        /// </summary>
        [HttpGet]
        public IActionResult StartMoMo([FromQuery] List<Guid> bookingIds)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            if (bookingIds == null || bookingIds.Count == 0)
            {
                TempData["ToastError"] = "Thiếu danh sách booking.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            decimal amount;
            try
            {
                amount = ComputeAmountFromSession(bookingIds);
            }
            catch (InvalidOperationException)
            {
                TempData["ToastError"] = "Phiên thanh toán đã hết hoặc không hợp lệ. Vui lòng đặt hàng lại.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            var orderInfo = $"Thanh toán {string.Join(",", bookingIds)}";

            var simulateSuccessUrl = Url.Action(nameof(MoMoReturn), "Payment",
                new
                {
                    area = "Customer",
                    result = "success",
                    resultCode = 0,
                    bookingIds = bookingIds,
                    amount = amount.ToString(CultureInfo.InvariantCulture),
                    orderInfo
                },
                Request.Scheme)!;

            var simulateFailUrl = Url.Action(nameof(MoMoReturn), "Payment",
                new
                {
                    area = "Customer",
                    result = "fail",
                    resultCode = 49,
                    message = "User canceled",
                    bookingIds = bookingIds,
                    amount = amount.ToString(CultureInfo.InvariantCulture),
                    orderInfo
                },
                Request.Scheme)!;

            var vm = new PaymentDemoViewModel
            {
                Gateway = "MoMo",
                BookingIds = bookingIds,
                Amount = amount,
                Message = "Đây là trang demo thanh toán MoMo.",
                SimulateSuccessUrl = simulateSuccessUrl,
                SimulateFailUrl = simulateFailUrl
            };
            return View("StartMoMo", vm);
        }

        public class PaymentDemoViewModel
        {
            public string Gateway { get; set; } = string.Empty;
            public List<Guid> BookingIds { get; set; } = new();
            public decimal Amount { get; set; }
            public string Message { get; set; } = string.Empty;
            public string SimulateSuccessUrl { get; set; } = string.Empty;
            public string SimulateFailUrl { get; set; } = string.Empty;
        }

        // ====== RETURN / IPN (DEMO) ======

        /// <summary>
        /// VNPay Return
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayReturn(
            [FromQuery] string? result,
            [FromQuery(Name = "vnp_ResponseCode")] string? vnpResponseCode,
            [FromQuery] List<Guid>? bookingIds,
            [FromQuery] decimal? amount,
            [FromQuery] string? message = null,
            CancellationToken ct = default)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            var isSuccess = string.Equals(result, "success", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(vnpResponseCode, "00", StringComparison.OrdinalIgnoreCase);

            if (isSuccess && bookingIds?.Any() == true)
            {
                try
                {
                    var verifiedAmount = ComputeAmountFromSession(bookingIds);
                    var jwt = HttpContext.Session.GetString("JWToken");

                    // Nếu flow từ giỏ hàng: lấy lại cartItemIds để backend cleanup
                    var csv = HttpContext.Session.GetString("CHECKOUT_SELECTED_IDS");
                    List<Guid>? cartItemIds = null;
                    if (!string.IsNullOrWhiteSpace(csv))
                    {
                        cartItemIds = csv
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                            .Where(g => g != Guid.Empty)
                            .Distinct()
                            .ToList();
                    }

                    await _bookingServiceCustomer.ConfirmPaymentsAsync(
                        new ConfirmPaymentsDto
                        {
                            BookingIds = bookingIds,
                            PaymentMethod = "VNPAY",
                            GatewayTxnId = $"VNPAY:{Guid.NewGuid():N}",
                            CartItemIdsForCleanup = cartItemIds
                        },
                        jwt,
                        ct);

                    // Dọn các flag checkout
                    HttpContext.Session.Remove("CHECKOUT_PENDING_BOOKING_IDS");
                    HttpContext.Session.Remove("CHECKOUT_DIRECT_JSON");
                    HttpContext.Session.Remove("CHECKOUT_SELECTED_IDS");

                    TempData["ToastSuccess"] = $"Thanh toán VNPay thành công (demo)! Số tiền: {verifiedAmount:0.##}";
                    return RedirectToAction(nameof(Success), new
                    {
                        area = "Customer",
                        bookingIds = bookingIds,
                        total = verifiedAmount,
                        gateway = "VNPAY"
                    });
                }
                catch (Exception ex)
                {
                    TempData["ToastError"] = "Ghi nhận thanh toán thất bại: " + ex.Message;
                    await CancelPendingFromIdsOrSessionAsync(bookingIds, ct);
                    return RedirectToAction("Index", "Cart", new { area = "Customer" });
                }
            }

            await CancelPendingFromIdsOrSessionAsync(bookingIds, ct);

            var reason = !string.IsNullOrWhiteSpace(message) ? message : $"Code={vnpResponseCode ?? "NA"}";
            TempData["ToastError"] = $"Thanh toán VNPay thất bại (demo): {reason}";
            return RedirectToAction("Index", "Cart", new { area = "Customer" });
        }

        /// <summary>
        /// MoMo Return
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MoMoReturn(
            [FromQuery] string? result,
            [FromQuery] int? resultCode,
            [FromQuery] List<Guid>? bookingIds,
            [FromQuery] decimal? amount,
            [FromQuery] string? message = null,
            CancellationToken ct = default)
        {
            if (NotLoggedIn())
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            var isSuccess = string.Equals(result, "success", StringComparison.OrdinalIgnoreCase)
                            || (resultCode.HasValue && resultCode.Value == 0);

            if (isSuccess && bookingIds?.Any() == true)
            {
                try
                {
                    var verifiedAmount = ComputeAmountFromSession(bookingIds);
                    var jwt = HttpContext.Session.GetString("JWToken");

                    var csv = HttpContext.Session.GetString("CHECKOUT_SELECTED_IDS");
                    List<Guid>? cartItemIds = null;
                    if (!string.IsNullOrWhiteSpace(csv))
                    {
                        cartItemIds = csv
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                            .Where(g => g != Guid.Empty)
                            .Distinct()
                            .ToList();
                    }

                    await _bookingServiceCustomer.ConfirmPaymentsAsync(
                        new ConfirmPaymentsDto
                        {
                            BookingIds = bookingIds,
                            PaymentMethod = "MOMO",
                            GatewayTxnId = $"MOMO:{Guid.NewGuid():N}",
                            CartItemIdsForCleanup = cartItemIds
                        },
                        jwt,
                        ct);

                    HttpContext.Session.Remove("CHECKOUT_PENDING_BOOKING_IDS");
                    HttpContext.Session.Remove("CHECKOUT_DIRECT_JSON");
                    HttpContext.Session.Remove("CHECKOUT_SELECTED_IDS");

                    TempData["ToastSuccess"] = $"Thanh toán MoMo thành công (demo)! Số tiền: {verifiedAmount:0.##}";
                    return RedirectToAction(nameof(Success), new
                    {
                        area = "Customer",
                        bookingIds = bookingIds,
                        total = verifiedAmount,
                        gateway = "MOMO"
                    });
                }
                catch (Exception ex)
                {
                    TempData["ToastError"] = "Ghi nhận thanh toán thất bại: " + ex.Message;
                    await CancelPendingFromIdsOrSessionAsync(bookingIds, ct);
                    return RedirectToAction("Index", "Cart", new { area = "Customer" });
                }
            }

            await CancelPendingFromIdsOrSessionAsync(bookingIds, ct);

            var reason = !string.IsNullOrWhiteSpace(message) ? message : $"Code={(resultCode?.ToString() ?? "NA")}";
            TempData["ToastError"] = $"Thanh toán MoMo thất bại (demo): {reason}";
            return RedirectToAction("Index", "Cart", new { area = "Customer" });
        }

        [HttpPost]
        public IActionResult MoMoIpn([FromBody] JsonElement payload)
        {
            bool success = false;

            if (payload.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.True)
            {
                success = true;
            }
            else if (payload.TryGetProperty("resultCode", out var rcProp) && rcProp.TryGetInt32(out var rc))
            {
                success = (rc == 0);
            }

            if (success) return Ok(new { message = "ok (demo)" });
            return BadRequest(new { message = "invalid (demo)" });
        }

        // ====== SUCCESS PAGE ======

        public class PaymentSuccessViewModel
        {
            public string Gateway { get; set; } = string.Empty; // VNPAY / MOMO
            public List<Guid> BookingIds { get; set; } = new();
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
                return RedirectToAction("Login", "Auth", new { area = "" });
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
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            await CancelPendingFromIdsOrSessionAsync(null, ct);
            return RedirectToAction("Index", "BookingService", new { area = "Customer", refresh = 1 });
        }

        // ====== VNPay Integration - Standard Methods ======

        /// <summary>
        /// Tạo URL thanh toán VNPay và chuyển hướng người dùng đến cổng thanh toán
        /// </summary>
        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Redirect(url);
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
                        PaymentTime = paymentTime // ✅ Gửi thời gian chính xác
                    },
                    jwt,
                    ct);

                TempData["ToastSuccess"] = $"Thanh toán VNPay thành công! Mã giao dịch: {transactionId}";
                
                // 🎉 Hiển thị trang success đẹp
                ViewBag.TransactionId = transactionId;
                ViewBag.BookingIds = bookingIdList;
                ViewBag.NeedLogin = false;
                
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
                    // Thanh toán thất bại
                    TempData["ToastError"] = $"Thanh toán VNPay thất bại. Mã lỗi: {response.VnPayResponseCode}";
                    
                    // Nếu chưa login, hiển thị thông báo
                    if (NotLoggedIn())
                    {
                        return Content($"Thanh toán thất bại. Mã lỗi: {response.VnPayResponseCode}. Vui lòng đăng nhập lại và thử lại.");
                    }
                    
                    // Hủy các booking đang chờ
                    await CancelPendingFromIdsOrSessionAsync(null, ct);
                    
                    return RedirectToAction("Index", "BookingService", new { area = "Customer", refresh = 1 });
                }

                // LẤY BOOKING IDS TỪ VNPAY RESPONSE TRƯỚC (không cần login)
                var jwt = HttpContext.Session.GetString("JWToken");
                
                // 🔑 Parse booking IDs từ OrderDescription (format: "BOOKINGS:guid1,guid2,guid3")
                var orderInfo = response.OrderDescription ?? "";
                List<Guid> bookingIds;
                
                // 🐛 DEBUG: Log để xem VNPay trả về gì
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
                        // ❌ Không tìm thấy ở cả 2 nơi → Hiển thị debug info
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

                // ✅ Kiểm tra đăng nhập NGAY TẠI ĐÂY (sau khi đã parse được booking IDs)
                if (NotLoggedIn())
                {
                    // 🎉 Hiển thị trang success đẹp thay vì redirect login
                    ViewBag.TransactionId = response.TransactionId;
                    ViewBag.BookingIds = bookingIds;
                    ViewBag.NeedLogin = true;
                    
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
                    // Nếu không lấy được từ session, lấy từ response
                    if (decimal.TryParse(response.OrderId, out var amt))
                    {
                        total = amt;
                    }
                }

                // Dọn session
                HttpContext.Session.Remove("CHECKOUT_PENDING_BOOKING_IDS");
                HttpContext.Session.Remove("CHECKOUT_DIRECT_JSON");
                HttpContext.Session.Remove("CHECKOUT_SELECTED_IDS");

                TempData["ToastSuccess"] = "Thanh toán VNPay thành công!";
                
                // 🎉 Hiển thị trang success đẹp
                ViewBag.TransactionId = response.TransactionId;
                ViewBag.BookingIds = bookingIds;
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
