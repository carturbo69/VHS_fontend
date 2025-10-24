using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Areas.Customer.Models.CartItemDTOs;
using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;
using VHS_frontend.Areas.Customer.Models.VoucherDTOs;
using VHS_frontend.Services.Customer;
using VHS_frontend.Services.Customer.Interfaces;
using VHS_frontend.Models.Payment;

namespace VHS_frontend.Areas.Customer.Controllers
{

    [Area("Customer")]
    public class BookingServiceController : Controller
    {
        private readonly CartServiceCustomer _cartService;
        private readonly BookingServiceCustomer _bookingServiceCustomer;
        private readonly IVnPayService _vnPayService;

        // Session keys để giữ lựa chọn trong flow checkout
        private const string SS_SELECTED_IDS = "CHECKOUT_SELECTED_IDS";
        private const string SS_VOUCHER_ID = "CHECKOUT_VOUCHER_ID";   // ✅ dùng VoucherId thay vì Code
        private const string SS_SELECTED_ADDR = "CHECKOUT_SELECTED_ADDRESS";

        private const string SS_PENDING_BOOKING_IDS = "CHECKOUT_PENDING_BOOKING_IDS";


        private const string SS_CHECKOUT_DIRECT = "CHECKOUT_DIRECT_JSON";

        public BookingServiceController(
            CartServiceCustomer cartService, 
            BookingServiceCustomer bookingServiceCustomer,
            IVnPayService vnPayService)
        {
            _cartService = cartService;
            _bookingServiceCustomer = bookingServiceCustomer;
            _vnPayService = vnPayService;
        }

        // Helper: lấy AccountId từ claim/session
        private Guid GetAccountId()
        {
            var idStr = User.FindFirstValue("AccountID") ?? HttpContext.Session.GetString("AccountID");
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }


        /// <summary>
        /// Gọi API lấy danh sách voucher trong giỏ hàng (dành cho khách hàng).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCartVouchers()
        {
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                var vouchers = await _cartService.GetCartVouchersAsync(jwtToken);
                return Ok(vouchers);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy voucher", error = ex.Message });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartDirect(Guid serviceId, List<Guid>? optionIds)
        {
            if (serviceId == Guid.Empty)
            {
                TempData["ToastError"] = "Dịch vụ không hợp lệ.";
                return RedirectToAction("Index", "Services", new { area = "Customer" });
            }

            var payload = new DirectCheckoutPayload(
                serviceId,
                (optionIds ?? new()).Where(x => x != Guid.Empty).Distinct().ToList()
            );

            HttpContext.Session.SetString(
                SS_CHECKOUT_DIRECT,
                System.Text.Json.JsonSerializer.Serialize(payload)
            );

            // Dọn dấu vết flow qua giỏ (để chắc chắn không lẫn)
            HttpContext.Session.Remove(SS_SELECTED_IDS);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Trang Checkout. Nhận VoucherId hoặc VoucherCode.
        /// Hỗ trợ cả luồng "Mua ngay" (SS_CHECKOUT_DIRECT) không đi qua giỏ.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? selectedKeysCsv, Guid? voucherId, string? voucherCode, bool refresh = false)
        {
            // ✅ Debug: Log parameters nhận được
            System.Diagnostics.Debug.WriteLine($"🎫 BookingService.Index received:");
            System.Diagnostics.Debug.WriteLine($"  - selectedKeysCsv: {selectedKeysCsv ?? "(null)"}");
            System.Diagnostics.Debug.WriteLine($"  - voucherId: {voucherId?.ToString() ?? "(null)"}");
            System.Diagnostics.Debug.WriteLine($"  - voucherCode: {voucherCode ?? "(null)"}");
            
            var jwt = HttpContext.Session.GetString("JWToken"); // 👈 kéo dòng này lên đầu để dùng cho cancel

            if (refresh)
            {
                HttpContext.Session.Remove("BookingBreakdownJson");
                await CancelPendingIfAnyAsync(jwt);             // 👈 THÊM DÒNG NÀY
            }

            // ====== Helpers ======
            static decimal LineTotalOf(ReadCartItemDTOs it)
                => (it?.ServicePrice ?? 0m) + (it?.Options?.Sum(o => o?.Price ?? 0m) ?? 0m);

            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            // ====== Lấy VoucherId từ session nếu không có trên query ======
            if (!voucherId.HasValue)
            {
                var voucherIdStr = HttpContext.Session.GetString(SS_VOUCHER_ID);
                if (Guid.TryParse(voucherIdStr, out var vid)) voucherId = vid;
            }
            
            // ====== Nếu không có voucherId nhưng có voucherCode, tìm VoucherId từ code ======
            if (!voucherId.HasValue && !string.IsNullOrWhiteSpace(voucherCode))
            {
                var allVouchers = await _cartService.GetCartVouchersAsync(jwt) ?? new List<ReadVoucherByCustomerDTOs>();
                var foundByCode = allVouchers.FirstOrDefault(v => 
                    string.Equals(v.Code, voucherCode, StringComparison.OrdinalIgnoreCase));
                if (foundByCode != null)
                {
                    voucherId = foundByCode.VoucherId;
                    System.Diagnostics.Debug.WriteLine($"  ✅ Found voucherId from code '{voucherCode}': {voucherId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  ❌ Could not find voucherId for code '{voucherCode}'");
                }
            }

            // ----------------------------------------------------------------
            // NHÁNH 1: DIRECT CHECKOUT (Mua ngay - không qua giỏ)
            // Chỉ kích hoạt khi không có selectedKeysCsv (tức không đi qua giỏ),
            // nhưng có session SS_CHECKOUT_DIRECT đã set bởi action StartDirect.
            // ----------------------------------------------------------------
            var directJson = HttpContext.Session.GetString(SS_CHECKOUT_DIRECT);
            if (string.IsNullOrWhiteSpace(selectedKeysCsv) && !string.IsNullOrWhiteSpace(directJson))
            {
                DirectCheckoutPayload? direct = null;
                try { direct = System.Text.Json.JsonSerializer.Deserialize<DirectCheckoutPayload>(directJson); }
                catch { /* ignore */ }

                if (direct == null || direct.ServiceId == Guid.Empty)
                {
                    TempData["ToastError"] = "Dữ liệu mua ngay không hợp lệ.";
                    return RedirectToAction("Index", "Services", new { area = "Customer" });
                }

                var svc = await _cartService.GetServiceDetailAsync(direct.ServiceId, jwt);
                if (svc == null)
                {
                    TempData["ToastError"] = "Không tìm thấy dịch vụ.";
                    return RedirectToAction("Index", "Services", new { area = "Customer" });
                }

                var optionIds = (direct.OptionIds ?? new()).Where(x => x != Guid.Empty).Distinct().ToList();
                var optList = optionIds.Any()
                    ? await _cartService.GetOptionsByIdsAsync(svc.ServiceId, optionIds, jwt) ?? new List<ReadServiceOptionDTOs>()
                    : new List<ReadServiceOptionDTOs>();

                var cartItems = new List<ReadCartItemDTOs>
        {
            new ReadCartItemDTOs
            {
                CartItemId   = Guid.NewGuid(),
                ServiceId    = svc.ServiceId,
                ServiceName  = svc.Title,
                ServicePrice = svc.Price,
                ProviderId   = svc.Provider?.ProviderId ?? Guid.Empty,
                ProviderName = svc.Provider?.ProviderName,
                Options      = optList.Select(o => new CartItemOptionReadDto
                {
                    OptionId   = o.OptionId,
                    OptionName = o.OptionName,
                    Price      = o.Price,
                    UnitType   = o.UnitType
                }).ToList()
            }
        };

                // Lấy profile + địa chỉ
                var (addresses, defaultAddrId) = await LoadUserAddressesAsync(accountId, jwt);
                var (fullName, phone) = await LoadUserProfileAsync(accountId, jwt);

                // Build VM
                var vm = BuildBookingVmFromCart(cartItems, /*voucherCode*/ null, fullName, phone, addresses, defaultAddrId);

                // ===== TÍNH TIỀN =====
                var subtotal = cartItems.Sum(LineTotalOf);

                // Voucher
                var vouchers = await _cartService.GetCartVouchersAsync(jwt) ?? new List<ReadVoucherByCustomerDTOs>();
                var chosen = voucherId.HasValue
                    ? vouchers.FirstOrDefault(v => v.VoucherId == voucherId.Value)
                    : null;

                decimal discount = 0m;
                decimal pctDec = 0m;
                decimal maxCap = 0m;
                if (chosen != null)
                {
                    pctDec = chosen.DiscountPercent ?? 0m;
                    maxCap = chosen.DiscountMaxAmount ?? 0m;
                    var raw = Math.Floor(subtotal * pctDec / 100m);
                    discount = maxCap > 0 ? Math.Min(raw, maxCap) : raw;
                    if (discount < 0) discount = 0;
                    if (discount > subtotal) discount = subtotal;
                }

                vm.Subtotal = subtotal;
                vm.VoucherDiscount = discount;
                vm.Total = Math.Max(0, subtotal - discount);

                // Breakdown cho UI
                vm.VoucherId = chosen?.VoucherId;
                vm.VoucherPercent = (int)Math.Round(pctDec);
                vm.VoucherMaxAmount = maxCap;

                // Lưu lại voucher cho PlaceOrder
                HttpContext.Session.SetString(SS_VOUCHER_ID, vm.VoucherId?.ToString() ?? "");
                // GIỮ SS_CHECKOUT_DIRECT để PlaceOrder biết đang đi nhánh direct (không set SS_SELECTED_IDS)
                HttpContext.Session.SetString(SS_CHECKOUT_DIRECT, directJson);

                return View("Index", vm);
            }

            // ----------------------------------------------------------------
            // NHÁNH 2: CHECKOUT TỪ GIỎ (giữ nguyên luồng cũ)
            // ----------------------------------------------------------------

            // ✅ Nếu thiếu query -> lấy lại từ Session
            if (string.IsNullOrWhiteSpace(selectedKeysCsv))
            {
                selectedKeysCsv = HttpContext.Session.GetString(SS_SELECTED_IDS);
            }

            if (string.IsNullOrWhiteSpace(selectedKeysCsv))
            {
                // Không có selectedIds và cũng không có direct => quay lại giỏ
                TempData["ToastError"] = "Vui lòng chọn ít nhất một dịch vụ.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            var ids = selectedKeysCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                TempData["ToastError"] = "Danh sách dịch vụ không hợp lệ.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            // Lấy chi tiết mục giỏ
            var cartItemsFromCart = await _cartService.GetCartItemsByIdsAsync(accountId, ids, jwt);
            if (cartItemsFromCart == null || cartItemsFromCart.Count == 0)
            {
                TempData["ToastError"] = "Không tìm thấy dịch vụ tương ứng.";
                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }

            // Lấy profile + địa chỉ
            {
                var (addresses, defaultAddrId) = await LoadUserAddressesAsync(accountId, jwt);
                var (fullName, phone) = await LoadUserProfileAsync(accountId, jwt);

                var vm = BuildBookingVmFromCart(cartItemsFromCart, /*voucherCode*/ null, fullName, phone, addresses, defaultAddrId);

                // ===== TÍNH TIỀN =====
                var subtotal = cartItemsFromCart.Sum(LineTotalOf);

                // Voucher
                var vouchers = await _cartService.GetCartVouchersAsync(jwt) ?? new List<ReadVoucherByCustomerDTOs>();
                var chosen = voucherId.HasValue
                    ? vouchers.FirstOrDefault(v => v.VoucherId == voucherId.Value)
                    : null;

                decimal discount = 0m;
                decimal pctDec = 0m; // %, dạng decimal
                decimal maxCap = 0m;

                if (chosen != null)
                {
                    pctDec = chosen.DiscountPercent ?? 0m;
                    maxCap = chosen.DiscountMaxAmount ?? 0m;
                    var raw = Math.Floor(subtotal * pctDec / 100m);
                    discount = maxCap > 0 ? Math.Min(raw, maxCap) : raw;
                    if (discount < 0) discount = 0;
                    if (discount > subtotal) discount = subtotal;
                }

                vm.Subtotal = subtotal;
                vm.VoucherDiscount = discount;
                vm.Total = Math.Max(0, subtotal - discount);

                // Breakdown cho UI
                vm.VoucherId = chosen?.VoucherId;
                vm.VoucherPercent = (int)Math.Round(pctDec);
                vm.VoucherMaxAmount = maxCap;
                
                // ✅ Debug: Log voucher được gán vào ViewModel
                System.Diagnostics.Debug.WriteLine($"  📋 ViewModel voucher info:");
                System.Diagnostics.Debug.WriteLine($"     - VoucherId: {vm.VoucherId?.ToString() ?? "(null)"}");
                System.Diagnostics.Debug.WriteLine($"     - VoucherPercent: {vm.VoucherPercent}%");
                System.Diagnostics.Debug.WriteLine($"     - VoucherMaxAmount: {vm.VoucherMaxAmount}");
                System.Diagnostics.Debug.WriteLine($"     - Discount: {discount}");
                if (chosen != null)
                {
                    System.Diagnostics.Debug.WriteLine($"     - Code: {chosen.Code}");
                }

                // Lưu session cho flow (CHỈ NHÁNH GIỎ)
                HttpContext.Session.SetString(SS_SELECTED_IDS, string.Join(',', ids));
                HttpContext.Session.SetString(SS_VOUCHER_ID, vm.VoucherId?.ToString() ?? "");

                // Dọn cờ direct nếu lỡ còn (đề phòng người dùng quay lại từ giỏ)
                HttpContext.Session.Remove(SS_CHECKOUT_DIRECT);

                return View("Index", vm);
            }
        }

        private async Task CancelPendingIfAnyAsync(string? jwt, CancellationToken ct = default)
        {
            var csv = HttpContext.Session.GetString(SS_PENDING_BOOKING_IDS);
            if (string.IsNullOrWhiteSpace(csv)) return;

            var pendingIds = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();

            if (pendingIds.Count == 0)
            {
                HttpContext.Session.Remove(SS_PENDING_BOOKING_IDS);
                return;
            }

            // Nếu chưa có JWT thì đừng gọi API, và GIỮ session để có thể hủy lại sau khi login
            if (string.IsNullOrWhiteSpace(jwt))
            {
                // Optional: TempData["ToastError"] = "Phiên đăng nhập đã hết hạn, không thể hủy đơn đang chờ.";
                return;
            }

            var canceledOk = false;
            try
            {
                await _bookingServiceCustomer.CancelUnpaidAsync(pendingIds, jwt, ct);
                canceledOk = true;
            }
            catch (HttpRequestException ex)
            {
                // Optional: log hoặc hiện toast rõ lý do (401/403/400/500)
                // _logger.LogWarning(ex, "CancelUnpaid failed");
                // TempData["ToastError"] = "Không thể hủy đơn đang chờ thanh toán. Vui lòng thử lại.";
            }
            catch (TaskCanceledException)
            {
                // Optional: _logger.LogWarning("CancelUnpaid timed out");
            }

            // ❗ Chỉ xóa flag khi HỦY THÀNH CÔNG
            if (canceledOk)
            {
                HttpContext.Session.Remove(SS_PENDING_BOOKING_IDS);
            }
        }




        ///// <summary>
        ///// Trang Checkout. Chỉ nhận VoucherId (không dùng code).
        ///// </summary>
        //[HttpGet]
        //public async Task<IActionResult> Index(string? selectedKeysCsv, Guid? voucherId)
        //{
        //    // ✅ Nếu thiếu query -> lấy lại từ Session
        //    if (string.IsNullOrWhiteSpace(selectedKeysCsv))
        //    {
        //        selectedKeysCsv = HttpContext.Session.GetString(SS_SELECTED_IDS);
        //    }

        //    if (string.IsNullOrWhiteSpace(selectedKeysCsv))
        //    {
        //        TempData["ToastError"] = "Vui lòng chọn ít nhất một dịch vụ.";
        //        return RedirectToAction("Index", "Cart", new { area = "Customer" });
        //    }

        //    var ids = selectedKeysCsv
        //        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //        .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
        //        .Where(g => g != Guid.Empty)
        //        .Distinct()
        //        .ToArray();

        //    if (ids.Length == 0)
        //    {
        //        TempData["ToastError"] = "Danh sách dịch vụ không hợp lệ.";
        //        return RedirectToAction("Index", "Cart", new { area = "Customer" });
        //    }

        //    var jwt = HttpContext.Session.GetString("JWToken");
        //    var accountId = GetAccountId();
        //    if (accountId == Guid.Empty)
        //    {
        //        TempData["ToastError"] = "Bạn cần đăng nhập.";
        //        return RedirectToAction("Login", "Auth", new { area = "" });
        //    }

        //    // Nếu không có voucherId từ query, lấy từ session
        //    if (!voucherId.HasValue)
        //    {
        //        var voucherIdStr = HttpContext.Session.GetString(SS_VOUCHER_ID);
        //        if (Guid.TryParse(voucherIdStr, out var vid)) voucherId = vid;
        //    }

        //    // Lấy chi tiết mục giỏ
        //    var cartItems = await _cartService.GetCartItemsByIdsAsync(accountId, ids, jwt);
        //    if (cartItems == null || cartItems.Count == 0)
        //    {
        //        TempData["ToastError"] = "Không tìm thấy dịch vụ tương ứng.";
        //        return RedirectToAction("Index", "Cart", new { area = "Customer" });
        //    }

        //    // Lấy profile + địa chỉ
        //    var (addresses, defaultAddrId) = await LoadUserAddressesAsync(accountId, jwt);
        //    var (fullName, phone) = await LoadUserProfileAsync(accountId, jwt);

        //    // Build VM cơ bản (⚠️ nếu hàm của bạn yêu cầu tham số voucherCode, truyền null)
        //    var vm = BuildBookingVmFromCart(cartItems, /*voucherCode:*/ null, fullName, phone, addresses, defaultAddrId);

        //    // ===== TÍNH TIỀN SERVER-SIDE =====
        //    static decimal LineTotalOf(ReadCartItemDTOs it)
        //        => (it?.ServicePrice ?? 0m) + (it?.Options?.Sum(o => o?.Price ?? 0m) ?? 0m);

        //    var subtotal = cartItems.Sum(LineTotalOf);

        //    // Lấy danh sách voucher rồi chọn theo VoucherId
        //    var vouchers = await _cartService.GetCartVouchersAsync(jwt) ?? new List<ReadVoucherByCustomerDTOs>();
        //    var chosen = voucherId.HasValue
        //        ? vouchers.FirstOrDefault(v => v.VoucherId == voucherId.Value)
        //        : null;

        //    decimal discount = 0m;
        //    decimal pctDec = 0m; // %, dạng decimal
        //    decimal maxCap = 0m;

        //    if (chosen != null)
        //    {
        //        pctDec = chosen.DiscountPercent ?? 0m;
        //        maxCap = chosen.DiscountMaxAmount ?? 0m;
        //        var raw = Math.Floor(subtotal * pctDec / 100m);
        //        discount = maxCap > 0 ? Math.Min(raw, maxCap) : raw;
        //        if (discount < 0) discount = 0;
        //        if (discount > subtotal) discount = subtotal;
        //    }

        //    vm.Subtotal = subtotal;
        //    vm.VoucherDiscount = discount;
        //    vm.Total = Math.Max(0, subtotal - discount);

        //    // Cho UI breakdown
        //    vm.VoucherId = chosen?.VoucherId;
        //    vm.VoucherPercent = (int)Math.Round(pctDec);
        //    vm.VoucherMaxAmount = maxCap;

        //    // Lưu session cho flow
        //    HttpContext.Session.SetString(SS_SELECTED_IDS, string.Join(',', ids));
        //    HttpContext.Session.SetString(SS_VOUCHER_ID, vm.VoucherId?.ToString() ?? "");

        //    return View("Index", vm);
        //}



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(BookingViewModel model, Dictionary<Guid, bool>? TosAccepted, CancellationToken ct)
        {
            var providerIds = (model.Items ?? new()).Select(x => x.ProviderId).Distinct().ToList();
            if (providerIds.Any(pid => TosAccepted == null || !TosAccepted.TryGetValue(pid, out var ok) || !ok))
            {
                TempData["ToastError"] = "Vui lòng đồng ý điều khoản của tất cả nhà cung cấp.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.SelectedPaymentCode))
            {
                TempData["ToastError"] = "Vui lòng chọn phương thức thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.AddressText))
            {
                TempData["ToastError"] = "Vui lòng chọn địa chỉ nhận hàng.";
                return RedirectToAction(nameof(Index));
            }

            var jwt = HttpContext.Session.GetString("JWToken");
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Auth", new { area = "" });
            }

            try
            {
                // MapFromViewModel đã đẩy VoucherId sang DTO
                var dto = BookingServiceCustomer.MapFromViewModel(model, accountId);
                var result = await _bookingServiceCustomer.CreateManyBookingsAsync(dto, jwt, ct);
                if (result == null || result.BookingIds?.Any() != true)
                {
                    TempData["ToastError"] = "Tạo đơn thất bại. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ LƯU BOOKING ĐANG CHỜ THANH TOÁN (để hủy nếu user quay lại / hủy thanh toán)
                HttpContext.Session.SetString(
                    SS_PENDING_BOOKING_IDS,
                    string.Join(",", result.BookingIds)
                );

                // Lưu để Success/COD hiển thị và Payment verify
                TempData["BookingIds"] = string.Join(",", result.BookingIds);
                TempData["Total"] = result.Total.ToString("0.##");
                TempData["Subtotal"] = result.Subtotal.ToString("0.##");
                TempData["Discount"] = result.Discount.ToString("0.##");

                HttpContext.Session.SetString(
                    "BookingBreakdownJson",
                    System.Text.Json.JsonSerializer.Serialize(result.Breakdown)
                );

                // ✨ Hiển thị số tiền ngay: truyền amount theo InvariantCulture
                var amountStr = result.Total.ToString(CultureInfo.InvariantCulture);

                switch (model.SelectedPaymentCode?.ToUpperInvariant())
                {
                    case "VNPAY":
                        // Tạo thông tin thanh toán cho VNPay
                        // 🔑 GỬI BOOKING IDS QUA VNPAY để tránh mất session khi redirect
                        var bookingIdsCsv = string.Join(",", result.BookingIds);
                        var vnpayPayment = new PaymentInformationModel
                        {
                            OrderType = "billpayment",
                            Amount = (double)result.Total,
                            OrderDescription = $"BOOKINGS:{bookingIdsCsv}",  // 🔑 Encode booking IDs vào đây
                            Name = model.RecipientFullName ?? "Khách hàng"
                        };

                        // Tạo URL VNPay và redirect trực tiếp
                        var vnpayUrl = _vnPayService.CreatePaymentUrl(vnpayPayment, HttpContext);
                        return Redirect(vnpayUrl);

                    case "MOMO":
                        return RedirectToAction(
                            "StartMoMo", "Payment",
                            new { area = "Customer", bookingIds = result.BookingIds, amount = amountStr });

                    default:
                        // COD
                        return RedirectToAction("Success");
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ToastError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Có lỗi khi tạo đơn." : ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException)
            {
                TempData["ToastError"] = "Yêu cầu bị hủy hoặc quá thời gian.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ToastError"] = "Đã xảy ra lỗi không xác định.";
                return RedirectToAction(nameof(Index));
            }
        }


        // Ở Dưới đây toàn là dữ liệu mẫu các nút gắn tương ứng với view chỉ cần sửa thêm api vào thôi

        // GET: /Customer/Address/Edit/{id}
        //[HttpGet]
        //public IActionResult Edit(Guid id)
        //{
        //    // TODO: load address từ DB
        //    var addr = BookingViewModel.AddressSample.FirstOrDefault(a => a.AddressId == id);
        //    if (addr == null) return NotFound();
        //    return View(addr);
        //}

        // POST: /Customer/Address/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, UserAddressDto model)
        {
            // TODO: update vào DB
            // Sau khi update thì redirect về trang thanh toán
            return RedirectToAction("Checkout", "Booking");
        }

        // POST: /Customer/Address/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            // TODO: xóa khỏi DB
            return RedirectToAction("Checkout", "Booking");
        }

      // Areas/Customer/Controllers/BookingServiceController.cs


[HttpGet]
        public IActionResult ListHistoryBooking()
        {
            // Tabs trạng thái theo đúng thứ tự trong ảnh
            var statuses = new[]
            {
        "Chờ xác nhận",
        "Xác Nhận",
        "Bắt Đầu Làm Việc",
        "Hoàn thành",
        "Đã hủy",
        "Báo Cáo/Hoàn tiền",
        "Tất cả"
    };

            var items = new List<BookingServiceItemDTO>
    {
        // Chờ xác nhận
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddHours(-6),
            Status = "Chờ xác nhận",
            Address = "12 Lý Thái Tổ, Q.10, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Intimate Ziaja Store",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Dung Dịch Vệ Sinh Intimate With Lactic Acid ZIAJA 200ml",
            ServicePrice = 209000, ServiceUnitType = "đ",
            ServiceImages = "/images/sample1.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Gói đóng gói an toàn", Description = "Bọc chống sốc + thùng carton", Price = 10000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Giao nhanh 2H",       Description = "Ưu tiên điều phối nhanh",     Price = 15000, UnitType = "đ" },
            }
        },
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddHours(-10),
            Status = "Chờ xác nhận",
            Address = "23 Hoàng Diệu, Q.4, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Green House",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Vệ sinh máy lạnh treo tường",
            ServicePrice = 150000, ServiceUnitType = "đ",
            ServiceImages = "/images/ac.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Vệ sinh dàn nóng", Description = "Bổ sung dàn nóng", Price = 50000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Bảo hành 7 ngày",  Description = "Quay lại xử lý",  Price = 20000, UnitType = "đ" },
            }
        },

        // Xác Nhận
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddHours(-5),
            Status = "Xác Nhận",
            Address = "456 Lê Duẩn, Q.1, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Xuân Vũ Audio",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Cáp thay thế tai nghe Moxpad X3 có mic",
            ServicePrice = 180000, ServiceUnitType = "đ",
            ServiceImages = "/images/sample2.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Bảo hành 12 tháng", Description = "1 đổi 1 trong 30 ngày", Price = 30000, UnitType = "đ" },
            }
        },

        // Bắt Đầu Làm Việc
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-1),
            Status = "Bắt Đầu Làm Việc",
            Address = "99 Nguyễn Thị Minh Khai, Q.1, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "HouseCare",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Vệ sinh sofa vải 3 chỗ",
            ServicePrice = 350000, ServiceUnitType = "đ",
            ServiceImages = "/images/sofa.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Khử khuẩn Nano Bạc", Description = "An toàn cho da", Price = 40000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Khử mùi Enzyme",     Description = "Loại bỏ mùi hôi", Price = 30000, UnitType = "đ" },
            }
        },

        // Hoàn thành
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-5),
            Status = "Hoàn thành",
            Address = "789 Trần Hưng Đạo, Q.3, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Intimate Ziaja Store",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Dung Dịch Vệ Sinh Intimate ZIAJA 200ml",
            ServicePrice = 209000, ServiceUnitType = "đ",
            ServiceImages = "/images/sample1.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Gói quà", Description = "Túi quà + thiệp", Price = 12000, UnitType = "đ" },
            }
        },

        // Đã hủy
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-3),
            Status = "Đã hủy",
            Address = "22 Ung Văn Khiêm, Bình Thạnh, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "CleanUp",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Vệ sinh nhà theo giờ (2h)",
            ServicePrice = 240000, ServiceUnitType = "đ",
            ServiceImages = "/images/clean.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Thêm 30 phút", Description = "Gia hạn thời gian", Price = 60000, UnitType = "đ" },
            }
        },

        // Báo Cáo/Hoàn tiền
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-4),
            Status = "Báo Cáo/Hoàn tiền",
            Address = "01 Võ Văn Ngân, TP.Thủ Đức",
            ProviderId = Guid.NewGuid(),
            ProviderName = "TechCare",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Sửa chữa – Vệ sinh laptop cơ bản",
            ServicePrice = 300000, ServiceUnitType = "đ",
            ServiceImages = "/images/laptop.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Thay keo tản nhiệt", Description = "Keo cao cấp", Price = 80000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Vệ sinh quạt",        Description = "Tháo vệ sinh kỹ", Price = 30000, UnitType = "đ" },
            }
        },
    };

            var vm = new ListHistoryBookingServiceDTOs { Items = items };
            ViewBag.StatusTabs = statuses;
            return View(vm); // View: Areas/Customer/Views/BookingService/ListHistoryBooking.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(CancelBookingRequestDTO model)
        {
            if (!ModelState.IsValid ||
                model.BookingId == Guid.Empty ||
                string.IsNullOrWhiteSpace(model.Reason) ||
                string.IsNullOrWhiteSpace(model.BankName) ||
                string.IsNullOrWhiteSpace(model.AccountHolderName) ||
                string.IsNullOrWhiteSpace(model.BankAccountNumber))
            {
                TempData["ToastError"] = "Vui lòng nhập đầy đủ thông tin hủy đơn.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                // TODO: Gọi API backend để hủy đơn + gửi thông tin hoàn tiền:
                // await _bookingService.CancelAsync(jwtToken, model);

                // Demo: giả lập thành công
                await Task.CompletedTask;

                TempData["ToastSuccess"] = "Hủy đơn thành công. Yêu cầu hoàn tiền sẽ được xử lý trong thời gian sớm nhất.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Bạn cần đăng nhập lại: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Không thể hủy đơn: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
        }

        // GET: /Customer/BookingService/ReportService/{bookingId}
        [HttpGet]
        public IActionResult ReportService(Guid bookingId)
        {
            // TODO: lấy thông tin thực từ DB/API theo bookingId
            // Dữ liệu mock để hiển thị
            var vm = new ComplaintDTO
            {
                BookingId = bookingId,
                ServiceTitle = "Dầu Tắm Oliv 3X Dưỡng Ẩm 650ml",
                ProviderName = "Oliv Official",
                Price = 108800,
                OriginalPrice = 197500,
                ServiceImage = "/images/sample1.png"
            };
            return View(vm); // View: Areas/Customer/Views/BookingService/ReportService.cshtml
        }

        // POST: /Customer/BookingService/SubmitReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(ComplaintDTO model)
        {
            if (model.BookingId == Guid.Empty ||
                string.IsNullOrWhiteSpace(model.ComplaintType) ||
                string.IsNullOrWhiteSpace(model.Description))
            {
                TempData["ToastError"] = "Vui lòng chọn lý do và nhập mô tả.";
                return RedirectToAction(nameof(ReportService), new { bookingId = model.BookingId });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                // TODO: Gọi API backend lưu khiếu nại (Complaint)
                // await _complaintService.CreateAsync(jwtToken, model);

                await Task.CompletedTask; // demo

                TempData["ToastSuccess"] = "Gửi báo cáo thành công. Hệ thống sẽ xử lý trong thời gian sớm nhất.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Bạn cần đăng nhập lại: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Không thể gửi báo cáo: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
        }

        // BookingServiceController

        // Có thể đặt ở đầu class (field static) để tái sử dụng
        private static readonly Dictionary<Guid, TermOfServiceDto> _demoTos = new()
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new TermOfServiceDto
            {
                ProviderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ProviderName = "Intimate Ziaja Store",
                Url = "https://example.com/ziaja-terms",
                Description = @"
            <ul>
                <li>Đổi trả trong 7 ngày đối với hàng chưa mở niêm phong.</li>
                <li>Sản phẩm mỹ phẩm tuân thủ quy định của Bộ Y Tế; bảo hành theo chính sách hãng.</li>
                <li>Giao nhanh nội thành TP.HCM 2–4 giờ (trong giờ làm việc).</li>
                <li>Vui lòng xem đầy đủ chính sách và ngoại lệ tại liên kết bên dưới.</li>
            </ul>",
                CreatedAt = DateTime.UtcNow
            },
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new TermOfServiceDto
            {
                ProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProviderName = "(GIFT) Quà Tặng Ziaja",
                Url = "https://example.com/gift-terms",
                Description = @"
            <ul>
                <li>Quà tặng không áp dụng bảo hành; chỉ đổi trong 3 ngày nếu lỗi sản xuất.</li>
                <li>Không hỗ trợ đổi vì lý do thẩm mỹ/chủ quan sau khi đã sử dụng.</li>
                <li>Voucher tặng kèm có thời hạn theo ghi chú trên voucher, không quy đổi tiền mặt.</li>
                <li>Chi tiết điều kiện sử dụng vui lòng xem tại liên kết bên dưới.</li>
            </ul>",
                CreatedAt = DateTime.UtcNow
            }
        };

        [HttpGet]
        public async Task<IActionResult> GetTermsByProvider(Guid providerId)
        {
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                // gọi service thật
                var tos = await _bookingServiceCustomer.GetTermOfServiceByProviderIdAsync(providerId, jwtToken);

                if (tos == null)
                {
                    // Backend trả 404 => hiển thị mặc định
                    tos = new VHS_frontend.Areas.Customer.Models.BookingServiceDTOs.TermOfServiceDto
                    {
                        ProviderId = providerId,
                        ProviderName = "Nhà cung cấp",
                        Url = "#",
                        Description = @"<p>Chưa có điều khoản cụ thể cho nhà cung cấp này.</p>",
                        CreatedAt = DateTime.UtcNow
                    };
                }

                var providerName = System.Net.WebUtility.HtmlEncode(tos.ProviderName ?? "Nhà cung cấp");

                var html = $@"
<div>
  <div style=""font-weight:600;margin-bottom:6px"">{providerName}</div>
  <div>{tos.Description}</div>
  <div style=""margin-top:8px"">
    <a href=""{tos.Url}"" target=""_blank"" rel=""noopener"">Xem đầy đủ điều khoản</a>
  </div>
</div>";

                return Content(html, "text/html; charset=utf-8");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải điều khoản", error = ex.Message });
            }
        }


        //// GET: /Customer/BookingService/HistoryBookingDetail/{id}
        //[HttpGet]
        //public IActionResult HistoryBookingDetail(Guid id)
        //{
        //    // Lấy dữ liệu mẫu có sẵn
        //    var vm = VHS_frontend.Areas.Customer.Models.BookingServiceDTOs.HistoryBookingDetailDTOs.Sample();

        //    // Gán lại BookingId theo id được click (cho “MÃ ĐƠN HÀNG”/route nhất quán)
        //    vm.BookingId = id;

        //    return View("HistoryBookingDetail", vm);
        //}

        // GET: /Customer/BookingService/HistoryBookingDetail/{id}?status=...
        [HttpGet]
        public IActionResult HistoryBookingDetail(Guid id, string? status)
        {
            // Tạo VM mẫu theo tab/status được click
            var vm = VHS_frontend.Areas.Customer.Models.BookingServiceDTOs.HistoryBookingDetailDTOs
                        .CreateByStatus(status ?? "Tất cả", id);

            return View("HistoryBookingDetail", vm);
        }

        public IActionResult CanceledDetail(Guid id)
        {
            // Lấy đơn đã hủy (demo dùng sample)
            var vm = HistoryBookingDetailDTOs.Sample_Canceled();
            vm.BookingId = id;

            // Thông tin hủy/hoàn tiền đã gửi lúc CancelBooking (demo)
            ViewBag.Cancel = new CancelBookingRequestDTO
            {
                BookingId = id,
                Reason = "Tôi muốn cập nhật địa chỉ/sđt nhận hàng",
                BankName = "Vietcombank",
                AccountHolderName = "NGUYEN VAN A",
                BankAccountNumber = "0123456789"
            };

            return View(vm); // Views/BookingService/CanceledDetail.cshtml
        }


        // Xem chi báo cáo hoàn tiền
        public IActionResult ReportDetail(Guid bookingId)
        {
            // TODO: Lấy ComplaintDTO thực tế từ DB
            var vm = new ComplaintDTO
            {
                BookingId = bookingId,
                ComplaintType = "Hàng/ dịch vụ không như mô tả",
                Description = "Ghế vệ sinh xong vẫn còn vết bẩn nhẹ ở tay vịn.",
                ServiceTitle = "Vệ sinh sofa vải 3 chỗ",
                ProviderName = "HouseCare",
                ServiceImage = "/images/sofa.png",
                OriginalPrice = 390000,
                Price = 350000
            };
            return View(vm);
        }

        // ===== Helpers: load dữ liệu thật =====

        private async Task<(List<UserAddressDto> list, Guid? defaultId)> LoadUserAddressesAsync(Guid accountId, string? jwt)
        {
            // TODO: gọi AddressService thật ở đây
            var list = new List<UserAddressDto>();

            // Fallback dùng địa chỉ mẫu khi chưa có service

            if (list.Count == 0)
                list = BookingViewModel.AddressSample();

            // Lấy id đã chọn từ session (nếu có)
            Guid? selected = null;
            var selFromSession = HttpContext.Session.GetString(SS_SELECTED_ADDR);
            if (Guid.TryParse(selFromSession, out var selId) && list.Any(a => a.AddressId == selId))
                selected = selId;
            else
                selected = list.FirstOrDefault()?.AddressId;

            return (list, selected);
        }


        private async Task<(string fullName, string phone)> LoadUserProfileAsync(Guid accountId, string? jwt)
        {
            // TODO: Gọi AccountService lấy hồ sơ KH
            // var p = await _acctService.GetProfileAsync(accountId, jwt);
            // return (p.FullName, p.Phone);
            return ("", ""); // nếu chưa có service, trả rỗng — view vẫn hiển thị được
        }

        private BookingViewModel BuildBookingVmFromCart(
     List<ReadCartItemDTOs> items,
     string? voucherCode,
     string fullName,
     string phone,
     List<UserAddressDto> addresses,
     Guid? defaultAddrId)
        {
            // Lấy địa chỉ được chọn (nếu có) hoặc địa chỉ đầu tiên
            var addr = (addresses ?? new()).FirstOrDefault(a => a.AddressId == defaultAddrId)
                       ?? (addresses ?? new()).FirstOrDefault();

            var vm = new BookingViewModel
            {
                RecipientFullName = fullName ?? "",
                RecipientPhone = phone ?? "",
                Addresses = addresses ?? new(),

                // Giữ object Address để hiển thị phần “Địa chỉ nhận hàng”
                Address = addr ?? new UserAddressDto(),

                // ✅ Chỉ dùng chuỗi snapshot để post về server khi PlaceOrder
                AddressText = addr?.ToDisplayString() ?? string.Empty,

                //VoucherCode = voucherCode,
                // Nếu không dùng ở view này thì bỏ hẳn block PaymentMethods:
                // PaymentMethods = new List<PaymentMethod>
                // {
                //     new() { Code = "COD",           DisplayName = "Thanh toán khi nhận hàng" },
                //     new() { Code = "BANK_TRANSFER", DisplayName = "Chuyển khoản ngân hàng" },
                // },

                // Để null để không auto-chọn gì cả
                SelectedPaymentCode = null
            };

            if (items != null)
            {
                foreach (var it in items)
                {
                    vm.Items.Add(new BookItem
                    {
                        CartItemId = it.CartItemId,
                        ServiceId = it.ServiceId,
                        ProviderId = it.ProviderId,
                        Provider = string.IsNullOrWhiteSpace(it.ProviderName) ? "Khác" : it.ProviderName,
                        ServiceName = string.IsNullOrWhiteSpace(it.ServiceName) ? "(Không có tên)" : it.ServiceName,
                        Image = string.IsNullOrWhiteSpace(it.ServiceImage) ? "/images/placeholder.png" : it.ServiceImage,
                        UnitPrice = it.ServicePrice ?? 0m,
                        BookingTime = DateTime.Now, // default, người dùng chỉnh ở UI
                        Options = (it.Options ?? new()).Select(o => new BookItemOption
                        {
                            OptionId = o.OptionId,
                            Name = o.OptionName ?? "",
                            Unit = o.UnitType,
                            Description = o.Description,
                            Price = o.Price
                        }).ToList()
                    });
                }
            }

            return vm;
        }

    }
}

