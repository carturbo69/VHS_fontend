using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class PaymentController : Controller
    {
        private readonly BookingServiceCustomer _bookingServiceCustomer;

        public PaymentController(BookingServiceCustomer bookingServiceCustomer)
        {
            _bookingServiceCustomer = bookingServiceCustomer;
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

    }
}
