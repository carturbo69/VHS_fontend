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

            // ====== PAGES DEMO ======

            /// <summary>
            /// Nhận List<Guid> bookingIds từ query, tự tính amount từ Session.Breakdown.
            /// </summary>
            [HttpGet]
            public IActionResult StartVnPay([FromQuery] List<Guid> bookingIds)
            {
                if (bookingIds == null || bookingIds.Count == 0)
                {
                    TempData["ToastError"] = "Thiếu danh sách booking.";
                    return RedirectToAction("Index", "BookingService", new { area = "Customer" });
                }

                // Tính tổng lại từ session breakdown
                var amount = ComputeAmountFromSession(bookingIds);

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
            /// Nhận List<Guid> bookingIds từ query, tự tính amount từ Session.Breakdown.
            /// </summary>
            [HttpGet]
            public IActionResult StartMoMo([FromQuery] List<Guid> bookingIds)
            {
                if (bookingIds == null || bookingIds.Count == 0)
                {
                    TempData["ToastError"] = "Thiếu danh sách booking.";
                    return RedirectToAction("Index", "BookingService", new { area = "Customer" });
                }

                var amount = ComputeAmountFromSession(bookingIds);

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
            /// VNPAY Return: nhận List<Guid>, (amount có thể bỏ qua hoặc dùng lại), vẫn nên tính lại để tránh query bị chỉnh tay.
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
                var isSuccess = string.Equals(result, "success", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(vnpResponseCode, "00", StringComparison.OrdinalIgnoreCase);

                if (isSuccess && bookingIds?.Any() == true)
                {
                    try
                    {
                        // Tính lại amount để tránh bị chỉnh query
                        var verifiedAmount = ComputeAmountFromSession(bookingIds);

                        var jwt = HttpContext.Session.GetString("JWToken");

                    // Lấy lại các CartItemId đã chọn từ Session (nếu flow giỏ)
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
                                GatewayTxnId = $"VNPAY:{Guid.NewGuid():N}", //này gom chung
                                CartItemIdsForCleanup = cartItemIds
                            },
                            jwt,
                            ct);

                        TempData["ToastSuccess"] = $"Thanh toán VNPay thành công (demo)! Số tiền: {verifiedAmount:0.##}";
                        // 🔁 Redirect về Payment/Success (KHÔNG còn BookingService/Success)
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
                        return RedirectToAction("Index", "BookingService", new { area = "Customer" });
                    }
                }

                var reason = !string.IsNullOrWhiteSpace(message) ? message : $"Code={vnpResponseCode ?? "NA"}";
                TempData["ToastError"] = $"Thanh toán VNPay thất bại (demo): {reason}";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
            }

            /// <summary>
            /// MoMo Return: giống VNPAY, nhận List<Guid> và tính lại.
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
                var isSuccess = string.Equals(result, "success", StringComparison.OrdinalIgnoreCase)
                                || (resultCode.HasValue && resultCode.Value == 0);

                if (isSuccess && bookingIds?.Any() == true)
                {
                    try
                    {
                        var verifiedAmount = ComputeAmountFromSession(bookingIds);

                        var jwt = HttpContext.Session.GetString("JWToken");

                    // Lấy lại các CartItemId đã chọn từ Session (nếu flow giỏ)
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

                        TempData["ToastSuccess"] = $"Thanh toán MoMo thành công (demo)! Số tiền: {verifiedAmount:0.##}";
                        // 🔁 Redirect về Payment/Success
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
                        return RedirectToAction("Index", "BookingService", new { area = "Customer" });
                    }
                }

                var reason = !string.IsNullOrWhiteSpace(message) ? message : $"Code={(resultCode?.ToString() ?? "NA")}";
                TempData["ToastError"] = $"Thanh toán MoMo thất bại (demo): {reason}";
                return RedirectToAction("Index", "BookingService", new { area = "Customer" });
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

            /// <summary>
            /// Trang hiển thị kết quả thành công sau khi cổng thanh toán trả về.
            /// Query: ?bookingIds=...&total=...&gateway=...
            /// </summary>
            [HttpGet]
            public IActionResult Success([FromQuery] List<Guid>? bookingIds, [FromQuery] decimal? total, [FromQuery] string? gateway)
            {
                var ids = bookingIds ?? new List<Guid>();
                var amt = total ?? 0m;

                // Fallback: nếu thiếu total nhưng có ids -> tính lại từ Session
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

            // ====== Helpers ======

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

            // DTO mirror với backend để deserialize breakdown trong session
            private class BookingAmountItem
            {
                public Guid BookingId { get; set; }
                public decimal Subtotal { get; set; }
                public decimal Discount { get; set; }
                public decimal Amount { get; set; }
            }
        }
    }
