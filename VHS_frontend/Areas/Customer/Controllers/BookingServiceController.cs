using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Globalization;
using System.Security.Claims;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Areas.Customer.Models.CartItemDTOs;
using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;
using VHS_frontend.Areas.Customer.Models.VoucherDTOs;
using VHS_frontend.Areas.Customer.Models.ReportDTOs;
using VHS_frontend.Services.Customer;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models.Staff;

namespace VHS_frontend.Areas.Customer.Controllers
{

    [Area("Customer")]
    public class BookingServiceController : Controller
    {
        private readonly CartServiceCustomer _cartService;
        private readonly BookingServiceCustomer _bookingServiceCustomer;
        private readonly UserAddressService _userAddressService;
        private readonly ReportService _reportService;
        private readonly StaffManagementService _staffService;

        // Session keys để giữ lựa chọn trong flow checkout
        private const string SS_SELECTED_IDS = "CHECKOUT_SELECTED_IDS";
        private const string SS_VOUCHER_ID = "CHECKOUT_VOUCHER_ID";   // ✅ dùng VoucherId thay vì Code
        private const string SS_SELECTED_ADDR = "CHECKOUT_SELECTED_ADDRESS";

        private const string SS_PENDING_BOOKING_IDS = "CHECKOUT_PENDING_BOOKING_IDS";


        private const string SS_CHECKOUT_DIRECT = "CHECKOUT_DIRECT_JSON";

        public BookingServiceController(
            CartServiceCustomer cartService, 
            BookingServiceCustomer bookingServiceCustomer, 
            UserAddressService userAddressService,
            ReportService reportService,
            StaffManagementService staffService)
        {
            _cartService = cartService;
            _bookingServiceCustomer = bookingServiceCustomer;
            _userAddressService = userAddressService;
            _reportService = reportService;
            _staffService = staffService;
        }

        // Helper: lấy AccountId từ claim/session với retry logic
        private Guid GetAccountId(bool retry = false)
        {
            // Thử lấy từ Claims trước (nhanh hơn)
            var idStr = User.FindFirstValue("AccountID");
            
            // Nếu không có trong Claims, thử lấy từ Session
            if (string.IsNullOrWhiteSpace(idStr))
            {
                idStr = HttpContext.Session.GetString("AccountID");
            }
            
            // Nếu vẫn không có và chưa retry, thử reload session
            if (string.IsNullOrWhiteSpace(idStr) && !retry)
            {
                // Load lại session để đảm bảo có dữ liệu mới nhất
                HttpContext.Session.LoadAsync().Wait(TimeSpan.FromMilliseconds(50));
                
                // Retry sau khi load
                return GetAccountId(retry: true);
            }
            
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
        /// Trang Checkout. Chỉ nhận VoucherId (không dùng code).
        /// Hỗ trợ cả luồng "Mua ngay" (SS_CHECKOUT_DIRECT) không đi qua giỏ.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? selectedKeysCsv, Guid? voucherId, bool refresh = false)
        {
            var jwt = HttpContext.Session.GetString("JWToken"); // 👈 kéo dòng này lên đầu để dùng cho cancel

            // ✅ Luôn kiểm tra và xóa booking chưa thanh toán khi vào trang này
            // (không chỉ khi refresh=1, để đảm bảo booking được dọn dẹp khi user quay lại)
            await CancelPendingIfAnyAsync(jwt);

            if (refresh)
            {
                HttpContext.Session.Remove("BookingBreakdownJson");
            }

            // ====== Helpers ======
            static decimal LineTotalOf(ReadCartItemDTOs it)
                => (it?.ServicePrice ?? 0m) + (it?.Options?.Sum(o => o?.Price ?? 0m) ?? 0m);

            // Kiểm tra JWToken trước - nếu có token thì có thể session đang load
            var hasJwt = !string.IsNullOrWhiteSpace(jwt);

            var accountId = GetAccountId();
            
            // Nếu không có accountId nhưng có JWT, có thể session chưa load kịp
            // Thử đợi một chút và retry
            if (accountId == Guid.Empty && hasJwt)
            {
                // Commit session để đảm bảo session được lưu
                await HttpContext.Session.CommitAsync();
                
                // Đợi một chút để session được load
                await Task.Delay(50);
                
                // Retry
                accountId = GetAccountId(retry: true);
            }
            
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // ====== Lấy VoucherId từ session nếu không có trên query ======
            if (!voucherId.HasValue)
            {
                var voucherIdStr = HttpContext.Session.GetString(SS_VOUCHER_ID);
                if (Guid.TryParse(voucherIdStr, out var vid)) voucherId = vid;
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
                ServiceImage = svc.Images, // Thêm service images
                ProviderId   = svc.Provider?.ProviderId ?? Guid.Empty,
                ProviderName = svc.Provider?.ProviderName,
                ProviderImages = svc.Provider?.Images, // Thêm provider images
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
                return RedirectToAction("Login", "Account", new { area = "" });
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

                // ✨ Lưu tên dịch vụ vào session để hiển thị ở trang Success
                var serviceNamesDict = new Dictionary<string, string>();
                if (model.Items != null && result.BookingIds != null)
                {
                    for (int i = 0; i < result.BookingIds.Count && i < model.Items.Count; i++)
                    {
                        var bookingId = result.BookingIds[i];
                        var item = model.Items[i];
                        var serviceName = !string.IsNullOrWhiteSpace(item.ServiceName) 
                            ? item.ServiceName 
                            : $"Dịch vụ số {i + 1}";
                        serviceNamesDict[bookingId.ToString()] = serviceName;
                    }
                }
                HttpContext.Session.SetString(
                    "BookingServiceNamesJson",
                    System.Text.Json.JsonSerializer.Serialize(serviceNamesDict)
                );

                // ✨ Hiển thị số tiền ngay: truyền amount theo InvariantCulture
                var amountStr = result.Total.ToString(CultureInfo.InvariantCulture);

                switch (model.SelectedPaymentCode?.ToUpperInvariant())
                {
                    case "VNPAY":
                        return RedirectToAction(
                            "StartVnPay", "Payment",
                            new { area = "Customer", bookingIds = result.BookingIds, amount = amountStr });

                    case "MOMO":
                        return RedirectToAction(
                            "StartMoMo", "Payment",
                            new { area = "Customer", bookingIds = result.BookingIds, amount = amountStr });

                    default:
                        // COD
                        return RedirectToAction("Success", "Payment", new { area = "Customer", bookingIds = result.BookingIds, total = result.Total });
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


        /// <summary>
        /// Đặt lại dịch vụ từ booking (đặt trực tiếp, không qua cart)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReorderBooking(Guid bookingId, Guid serviceId, List<Guid>? optionIds)
        {
            if (serviceId == Guid.Empty)
            {
                TempData["ToastError"] = "Dịch vụ không hợp lệ.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            // Lưu thông tin vào session để đặt trực tiếp (giống StartDirect)
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

            // Redirect đến trang booking
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ListHistoryBooking(CancellationToken ct)
        {
            var statusOrder = new[] { "All", "Pending", "Confirmed", "InProgress", "Completed", "Cancelled" };

            var statusViMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Pending"] = "Chờ xác nhận",
                ["Confirmed"] = "Xác Nhận",
                ["InProgress"] = "Bắt Đầu Làm Việc",
                ["Completed"] = "Hoàn thành",
                ["Cancelled"] = "Đã hủy",
                ["All"] = "Tất cả"
            };

            Guid accountId;
            if (Request.Query.TryGetValue("accountId", out var q) && Guid.TryParse(q.ToString(), out var fromQuery))
                accountId = fromQuery;
            else
                accountId = GetAccountId();

            if (accountId == Guid.Empty)
                return Unauthorized("Không tìm thấy accountId. Vui lòng đăng nhập hoặc truyền accountId qua query.");

            var jwt = HttpContext.Session.GetString("JWToken");

            ListHistoryBookingServiceDTOs vm;
            try
            {
                vm = await _bookingServiceCustomer.GetHistoryByAccountAsync(accountId, jwt, ct)
                     ?? new ListHistoryBookingServiceDTOs { Items = new() };
            }
            catch (HttpRequestException ex)
            {
                vm = new ListHistoryBookingServiceDTOs { Items = new() };
            }

            ViewBag.StatusOrder = statusOrder;
            ViewBag.StatusViMap = statusViMap;
            return View("ListHistoryBooking", vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(CancelBookingRequestDTO model)
        {
            if (!ModelState.IsValid ||
                model.BookingId == Guid.Empty ||
                string.IsNullOrWhiteSpace(model.Reason) ||
                string.IsNullOrWhiteSpace(model.BankName) ||
                string.IsNullOrWhiteSpace(model.BankAccountNumber))
            {
                TempData["ToastError"] = "Vui lòng nhập đầy đủ thông tin hủy đơn.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
                var accountId = GetAccountId();
                if (accountId == Guid.Empty)
                {
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
                var createRefundReq = new {
                    BookingId = model.BookingId,
                    BankAccount = model.BankAccountNumber,
                    BankName = model.BankName,
                    AccountHolderName = model.AccountHolderName,
                    CancelReason = model.Reason
                };
                // Giả định _bookingServiceCustomer đã cập nhật API call phù hợp BE
                // Kiểm tra lại status trước khi submit (để tránh submit nhầm)
                // Sử dụng cả NormalizedStatus và Status gốc để xử lý cả tiếng Anh và tiếng Việt
                HistoryBookingDetailDTO? bookingDetail = null;
                try
                {
                    bookingDetail = await _bookingServiceCustomer.GetHistoryDetailAsync(accountId, model.BookingId, jwtToken);
                }
                catch (HttpRequestException ex)
                {
                    TempData["ToastError"] = $"Không thể kiểm tra trạng thái đơn hàng: {ex.Message}";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
                
                if (bookingDetail != null)
                {
                    var rawStatus = (bookingDetail.Status ?? "").Trim();
                    var normalizedStatus = bookingDetail.NormalizedStatus;
                    
                    // Kiểm tra xem có phải Pending không (cả tiếng Anh và các biến thể tiếng Việt)
                    var isPending = normalizedStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                                    rawStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                                    rawStatus.Equals("Chờ xác nhận", StringComparison.OrdinalIgnoreCase) ||
                                    rawStatus.Equals("Đang chờ xử lý", StringComparison.OrdinalIgnoreCase) ||
                                    rawStatus.Equals("Chờ xử lý", StringComparison.OrdinalIgnoreCase);

                    if (!isPending)
                    {
                        // Lấy status tiếng Việt để hiển thị
                        var statusVi = bookingDetail.StatusVi;
                        if (string.IsNullOrWhiteSpace(statusVi) || statusVi == "—")
                        {
                            statusVi = bookingDetail.NormalizedStatus switch
                            {
                                "Confirmed" => "Đã xác nhận",
                                "InProgress" => "Đang thực hiện",
                                "Completed" => "Đã hoàn thành",
                                "Cancelled" => "Đã hủy",
                                _ => rawStatus // Hiển thị status gốc nếu không map được
                            };
                        }
                        TempData["ToastError"] = $"Không thể hủy đơn này. Chỉ có thể hủy đơn ở trạng thái 'Chờ xác nhận'. Đơn hiện tại đang ở trạng thái: {statusVi}.";
                        return RedirectToAction(nameof(HistoryBookingDetail), new { id = model.BookingId });
                    }
                }

                var result = await _bookingServiceCustomer.CancelBookingWithRefundFullAsync(createRefundReq, jwtToken);
                if (result?.Success == true)
                {
                    TempData["ToastSuccess"] = "Hủy đơn thành công. Yêu cầu hoàn tiền sẽ được xử lý trong thời gian sớm nhất.";
                }
                else
                {
                    // Parse message từ backend để hiển thị rõ ràng hơn
                    var errorMsg = result?.Message ?? "Không thể hủy đơn. Vui lòng thử lại.";
                    if (errorMsg.Contains("Only pending bookings can be cancelled"))
                    {
                        errorMsg = "Chỉ có thể hủy đơn ở trạng thái 'Chờ xác nhận'. Vui lòng kiểm tra lại trạng thái đơn hàng.";
                    }
                    TempData["ToastError"] = errorMsg;
                    return RedirectToAction(nameof(HistoryBookingDetail), new { id = model.BookingId });
                }
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (HttpRequestException ex)
            {
                // Parse error message từ backend
                var errorMsg = ex.Message;
                if (errorMsg.Contains("Only pending bookings can be cancelled"))
                {
                    errorMsg = "Chỉ có thể hủy đơn ở trạng thái 'Chờ xác nhận'. Đơn hàng của bạn có thể đã được xác nhận hoặc đang ở trạng thái khác.";
                    TempData["ToastError"] = errorMsg;
                    // Redirect về chi tiết đơn để user thấy trạng thái hiện tại
                    return RedirectToAction(nameof(HistoryBookingDetail), new { id = model.BookingId });
                }
                TempData["ToastError"] = $"Không thể hủy đơn: {errorMsg}";
                return RedirectToAction(nameof(HistoryBookingDetail), new { id = model.BookingId });
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Không thể hủy đơn: {ex.Message}";
                return RedirectToAction(nameof(HistoryBookingDetail), new { id = model.BookingId });
            }
        }

        // GET: /Customer/BookingService/ReportService/{bookingId}
        [HttpGet]
        public async Task<IActionResult> ReportService(Guid bookingId, CancellationToken ct)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            try
            {
                // Lấy thông tin booking detail
                var jwtToken = HttpContext.Session.GetString("JWToken");
                HistoryBookingDetailDTO? bookingDetail = null;
                try
                {
                    bookingDetail = await _bookingServiceCustomer.GetHistoryDetailAsync(accountId, bookingId, jwtToken);
                }
                catch (HttpRequestException ex)
                {
                    TempData["ToastError"] = $"Không thể tải thông tin đơn hàng: {ex.Message}";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
                
                if (bookingDetail == null)
                {
                    TempData["ToastError"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }

                // Tính toán giá tiền (giống ListHistoryBooking)
                var optionTotal = bookingDetail.Options?.Sum(op => op.Price) ?? 0m;
                var servicePrice = bookingDetail.Service?.UnitPrice ?? 0m;
                var subTotal = servicePrice + optionTotal;
                
                // Lấy voucher discount trực tiếp từ DTO (giống ListHistoryBooking: o.VoucherDiscount)
                var voucherDiscount = Math.Max(0m, bookingDetail.VoucherDiscount); // bảo vệ số âm
                var grandTotal = Math.Max(0m, subTotal - voucherDiscount);

                // Xác định trạng thái thanh toán dựa trên PaymentStatus
                var paidAmount = bookingDetail.PaidAmount;
                var paymentStatusText = "Chưa thanh toán";
                
                // Kiểm tra PaymentStatus (ưu tiên) hoặc PaidAmount
                var paymentStatus = bookingDetail.PaymentStatus;
                if (!string.IsNullOrWhiteSpace(paymentStatus))
                {
                    var statusUpper = paymentStatus.Trim().ToUpperInvariant();
                    if (statusUpper == "ĐÃ THANH TOÁN" || 
                        statusUpper == "PAID" || 
                        statusUpper == "SUCCESS" || 
                        statusUpper == "COMPLETED")
                    {
                        paymentStatusText = "Đã thanh toán";
                    }
                    else if (statusUpper == "REFUNDED")
                    {
                        paymentStatusText = "Đã hoàn tiền";
                    }
                    else if (statusUpper == "PENDING")
                    {
                        paymentStatusText = "Chờ thanh toán";
                    }
                }
                else if (paidAmount > 0)
                {
                    // Fallback: nếu không có PaymentStatus nhưng có PaidAmount > 0
                    paymentStatusText = "Đã thanh toán";
                }

                // Tạo ViewModel với dữ liệu thực
                var vm = new ReportServiceViewModel
                {
                    BookingId = bookingId,
                    ProviderId = bookingDetail.ProviderId,
                    ServiceTitle = bookingDetail.Service?.Title ?? "Dịch vụ",
                    ProviderName = bookingDetail.ProviderName ?? "Provider",
                    ProviderImages = bookingDetail.ProviderImages,
                    ServiceImage = bookingDetail.Service?.Image ?? "/images/VeSinh.jpg",
                    ServiceImages = bookingDetail.ServiceImages,
                    ServicePrice = servicePrice,
                    OptionTotal = optionTotal,
                    SubTotal = subTotal,
                    VoucherDiscount = voucherDiscount,
                    GrandTotal = grandTotal,
                    PaidAmount = paidAmount,
                    PaymentStatus = paymentStatusText,
                    Options = bookingDetail.Options ?? new List<OptionDTO>(),
                    Price = grandTotal, // Legacy
                    OriginalPrice = servicePrice, // Legacy
                    ReportTypes = _reportService.GetReportTypes()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(ListHistoryBooking));
            }
        }

        // POST: /Customer/BookingService/SubmitReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(
            [FromForm] Guid BookingId,
            [FromForm] ReportTypeEnum ReportType,
            [FromForm] string Title,
            [FromForm] string? Description,
            [FromForm] Guid? ProviderId,
            [FromForm] List<IFormFile>? Attachments,
            CancellationToken ct)
        {
            if (BookingId == Guid.Empty || string.IsNullOrWhiteSpace(Title))
            {
                TempData["ToastError"] = "Vui lòng điền đầy đủ thông tin báo cáo.";
                return RedirectToAction(nameof(ReportService), new { bookingId = BookingId });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return RedirectToAction(nameof(ListHistoryBooking));
                }

                var createDto = new CreateReportDTO
                {
                    BookingId = BookingId,
                    ReportType = ReportType,
                    Title = Title,
                    Description = Description,
                    ProviderId = ProviderId,
                    Attachments = Attachments
                };

                var result = await _reportService.CreateReportAsync(createDto, jwtToken, ct);

                if (result != null)
                {
                    TempData["ToastSuccess"] = "Gửi báo cáo thành công. Hệ thống sẽ xử lý trong thời gian sớm nhất.";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
                else
                {
                    TempData["ToastError"] = "Không thể tạo báo cáo. Vui lòng thử lại.";
                    return RedirectToAction(nameof(ReportService), new { bookingId = BookingId });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (HttpRequestException ex)
            {
                // Parse error message for special cases
                if (ex.Message.Contains("Booking đã được báo cáo trước đó"))
                {
                    TempData["DuplicateReportError"] = "Booking đã được báo cáo trước đó. Mỗi đơn hàng chỉ được báo cáo 1 lần.";
                }
                return RedirectToAction(nameof(ReportService), new { bookingId = BookingId });
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(ReportService), new { bookingId = BookingId });
            }
        }

        //// BookingServiceController

        //// Có thể đặt ở đầu class (field static) để tái sử dụng
        //private static readonly Dictionary<Guid, TermOfServiceDto> _demoTos = new()
        //{
        //    [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new TermOfServiceDto
        //    {
        //        ProviderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        //        ProviderName = "Intimate Ziaja Store",
        //        Url = "https://example.com/ziaja-terms",
        //        Description = @"
        //    <ul>
        //        <li>Đổi trả trong 7 ngày đối với hàng chưa mở niêm phong.</li>
        //        <li>Sản phẩm mỹ phẩm tuân thủ quy định của Bộ Y Tế; bảo hành theo chính sách hãng.</li>
        //        <li>Giao nhanh nội thành TP.HCM 2–4 giờ (trong giờ làm việc).</li>
        //        <li>Vui lòng xem đầy đủ chính sách và ngoại lệ tại liên kết bên dưới.</li>
        //    </ul>",
        //        CreatedAt = DateTime.UtcNow
        //    },
        //    [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new TermOfServiceDto
        //    {
        //        ProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        //        ProviderName = "(GIFT) Quà Tặng Ziaja",
        //        Url = "https://example.com/gift-terms",
        //        Description = @"
        //    <ul>
        //        <li>Quà tặng không áp dụng bảo hành; chỉ đổi trong 3 ngày nếu lỗi sản xuất.</li>
        //        <li>Không hỗ trợ đổi vì lý do thẩm mỹ/chủ quan sau khi đã sử dụng.</li>
        //        <li>Voucher tặng kèm có thời hạn theo ghi chú trên voucher, không quy đổi tiền mặt.</li>
        //        <li>Chi tiết điều kiện sử dụng vui lòng xem tại liên kết bên dưới.</li>
        //    </ul>",
        //        CreatedAt = DateTime.UtcNow
        //    }
        //};


        [HttpGet]
        public async Task<IActionResult> GetTermsByProvider(Guid providerId)
        {
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                var tos = await _bookingServiceCustomer.GetTermOfServiceByProviderIdAsync(providerId, jwtToken);

                if (tos == null)
                {
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
  <div>{providerName}</div>
  <div class=""tos-content"">{tos.Description}</div>
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


        [HttpGet]
        public async Task<IActionResult> HistoryBookingDetail(Guid id, string? status)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty) return Unauthorized();

            var fourStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Pending", "Confirmed", "InProgress", "Completed" };

            ViewBag.StatusViMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Pending"] = "Chờ xác nhận",
                ["Confirmed"] = "Xác Nhận",
                ["InProgress"] = "Bắt Đầu Làm Việc",
                ["Completed"] = "Hoàn thành",
                ["Cancelled"] = "Đã hủy"
            };

            HistoryBookingDetailDTO? vm = null;
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                vm = await _bookingServiceCustomer.GetHistoryDetailAsync(accountId, id, jwtToken);
                if (vm == null)
                {
                    TempData["ToastError"] = "Không tìm thấy thông tin đơn hàng.";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
                // Bổ sung thông tin liên hệ nhân viên nếu có StaffId
                if (vm.StaffId.HasValue)
                {
                    try
                    {
                        var staff = await _staffService.GetStaffByIdAsync(vm.StaffId.Value.ToString(), jwtToken);
                        if (staff != null)
                        {
                            if (string.IsNullOrWhiteSpace(vm.StaffPhone)) vm.StaffPhone = staff.PhoneNumber ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(vm.StaffAddress)) vm.StaffAddress = staff.Address ?? string.Empty;
                        }
                    }
                    catch { /* ignore if cannot load staff */ }
                }
                // Đưa ra ViewBag để view có thể fallback, chuẩn hóa về null khi trống
                ViewBag.StaffPhone = string.IsNullOrWhiteSpace(vm.StaffPhone) ? null : vm.StaffPhone;
                ViewBag.StaffAddress = string.IsNullOrWhiteSpace(vm.StaffAddress) ? null : vm.StaffAddress;
            }
            catch (HttpRequestException ex)
            {
                // Xử lý lỗi từ backend (400, 500, etc.)
                var errorMessage = "Không thể tải thông tin đơn hàng. ";
                if (ex.Message.Contains("500") || ex.Message.Contains("Internal Server Error"))
                {
                    errorMessage += "Có lỗi xảy ra ở server. Vui lòng thử lại sau.";
                }
                else if (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                {
                    errorMessage += "Không tìm thấy đơn hàng.";
                }
                else if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                {
                    errorMessage += "Bạn cần đăng nhập lại.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }
                else
                {
                    errorMessage += ex.Message;
                }
                
                TempData["ToastError"] = errorMessage;
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                TempData["ToastError"] = "Đã xảy ra lỗi không xác định khi tải thông tin đơn hàng.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            var currentStatus = vm.NormalizedStatus;

            if (!string.IsNullOrWhiteSpace(status) && fourStates.Contains(status.Trim()))
                currentStatus = status.Trim();

            ViewBag.CurrentStatus = currentStatus;
            ViewBag.FourStates = fourStates;

            // Check if booking has a report
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(jwtToken))
                {
                    var (hasReport, report) = await _reportService.CheckBookingHasReportAsync(id, jwtToken);
                    ViewBag.HasReport = hasReport;
                    ViewBag.ReportId = report?.ComplaintId;
                }
                else
                {
                    ViewBag.HasReport = false;
                }
            }
            catch
            {
                ViewBag.HasReport = false;
            }

            return View("HistoryBookingDetail", vm);
        }



        public async Task<IActionResult> CanceledDetail(Guid id, CancellationToken ct)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
                return Unauthorized();

            // Lấy thông tin booking đã hủy từ service (bao gồm refund info)
            HistoryBookingDetailDTO? vm = null;
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                vm = await _bookingServiceCustomer.GetHistoryDetailAsync(accountId, id, jwtToken);
                if (vm == null)
                {
                    TempData["ToastError"] = "Không tìm thấy thông tin đơn hàng.";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["ToastError"] = $"Không thể tải thông tin đơn hàng: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = "Đã xảy ra lỗi không xác định khi tải thông tin đơn hàng.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            return View(vm);
        }


        public async Task<IActionResult> ReportDetail(Guid reportId, CancellationToken ct)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return RedirectToAction(nameof(ListHistoryBooking));
                }

                var report = await _reportService.GetReportByIdAsync(reportId, jwtToken, ct);
                if (report == null)
                {
                    TempData["ToastError"] = "Không tìm thấy báo cáo.";
                    return RedirectToAction(nameof(ListHistoryBooking));
                }

                return View(report);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(ListHistoryBooking));
            }
        }

        // ===== Helpers: load dữ liệu thật =====

        private async Task<(List<UserAddressDto> list, Guid? defaultId)> LoadUserAddressesAsync(Guid accountId, string? jwt)
        {
            // Gọi API UserAddress từ database
            if (string.IsNullOrWhiteSpace(jwt))
            {
                // Không có JWT - trả về rỗng
                return (new List<UserAddressDto>(), null);
            }

            try
            {
                // Gọi API thật từ backend
                var list = await _userAddressService.GetUserAddressesAsync(jwt);

                // Lấy id đã chọn từ session (nếu có)
                Guid? selected = null;
                var selFromSession = HttpContext.Session.GetString(SS_SELECTED_ADDR);
                if (Guid.TryParse(selFromSession, out var selId) && list.Any(a => a.AddressId == selId))
                    selected = selId;
                else
                    selected = list.FirstOrDefault()?.AddressId;

                return (list, selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading addresses from API: {ex.Message}");
                return (new List<UserAddressDto>(), null);
            }
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
                        ServiceImages = it.ServiceImage, // Sử dụng ServiceImage làm ServiceImages
                        ProviderImages = it.ProviderImages, // Lấy từ cart item DTO
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

