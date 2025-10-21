using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using VHS_frontend.Areas.Customer.Models.CartItemDTOs;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {

        private readonly CartServiceCustomer _cartService;

        public CartController(CartServiceCustomer cartService)
        {
            _cartService = cartService;
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

        /// <summary>
        /// Lấy tổng số dịch vụ trong giỏ hàng (CartItem Count)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCartTotalCount()
        {
            try
            {
                var accountIdString = HttpContext.Session.GetString("AccountID");
                var jwtToken = HttpContext.Session.GetString("JWToken");

                if (string.IsNullOrEmpty(accountIdString) || !Guid.TryParse(accountIdString, out Guid accountId))
                {
                    return Json(new { total = 0 });
                }

                var totalDto = await _cartService.GetTotalCartItemAsync(accountId, jwtToken);

                return Json(new { total = totalDto.TotalServices });
            }
            catch
            {
                return Json(new { total = 0 });
            }
        }

        public async Task<IActionResult> Index()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwt = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var items = await _cartService.GetCartItemsByAccountIdAsync(accountId, jwt);

                // NEW: Lấy tất cả option của các service trong giỏ để làm "Gợi ý thêm"
                var serviceIds = items.Select(i => i.ServiceId).Distinct().ToList();
                var dictByService = new Dictionary<Guid, List<VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs.ReadServiceOptionDTOs>>();

                foreach (var sid in serviceIds)
                {
                    var opts = await _cartService.GetAllOptionsByServiceIdAsync(sid)
                               ?? new List<VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs.ReadServiceOptionDTOs>();
                    dictByService[sid] = opts;
                }

                ViewBag.AvailableOptionsByServiceId = dictByService;

                return View(items); // Model: List<ReadCartItemDTOs>
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Không thể tải giỏ hàng: {ex.Message}";
                return View(new List<ReadCartItemDTOs>());
            }
        }

        /// <summary>
        /// Thêm service vào giỏ hàng (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid serviceId, List<Guid>? optionIds)
        {
            // Lấy accountId & token từ Session
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwtToken = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để thêm vào giỏ.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var req = new AddCartItemRequest
                {
                    ServiceId = serviceId,
                    OptionIds = optionIds ?? new List<Guid>()
                };

                var ok = await _cartService.AddItemToCartAsync(accountId, req, jwtToken);

                if (!ok)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = "Thêm vào giỏ hàng thất bại.";
                }
                else
                {
                    TempData["ToastType"] = "success";
                    TempData["ToastMessage"] = "Đã thêm vào giỏ hàng.";
                }

                // Điều hướng về trang giỏ (tuỳ ý bạn đổi nơi quay lại)
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Không thể thêm vào giỏ: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===== XÓA 1 ITEM =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid key)
        {
            // key = CartItemId
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwt = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để xóa sản phẩm.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var ok = await _cartService.RemoveCartItemAsync(accountId, key, jwt);
                TempData["ToastType"] = ok ? "success" : "error";
                TempData["ToastMessage"] = ok ? "Đã xóa sản phẩm khỏi giỏ." : "Không thể xóa sản phẩm.";
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Lỗi khi xóa sản phẩm: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== XÓA NHIỀU ITEM (BULK) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSelected([FromForm] List<Guid> selectedIds)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwt = HttpContext.Session.GetString("JWToken");

            if (selectedIds == null || selectedIds.Count == 0)
            {
                TempData["ToastType"] = "info";
                TempData["ToastMessage"] = "Bạn chưa chọn sản phẩm nào để xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để xóa sản phẩm.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var ok = await _cartService.RemoveCartItemsAsync(accountId, selectedIds, jwt);
                TempData["ToastType"] = ok ? "success" : "error";
                TempData["ToastMessage"] = ok ? "Đã xóa các sản phẩm đã chọn." : "Không thể xóa các sản phẩm đã chọn.";
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Lỗi khi xóa nhiều sản phẩm: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== XÓA TOÀN BỘ GIỎ =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwt = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để xóa giỏ hàng.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            try
            {
                var ok = await _cartService.ClearCartAsync(accountId, jwt);
                TempData["ToastType"] = ok ? "success" : "error";
                TempData["ToastMessage"] = ok ? "Đã xóa toàn bộ giỏ hàng." : "Không thể xóa giỏ hàng.";
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Lỗi khi xóa giỏ hàng: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== THÊM TUỲ CHỌN VÀO CART ITEM =====
        // Form ở View gửi: cartItemId + optionIds[]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddon(Guid cartItemId, List<Guid>? optionIds)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwt = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để thêm tuỳ chọn.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (cartItemId == Guid.Empty || optionIds == null || optionIds.Count == 0)
            {
                TempData["ToastType"] = "info";
                TempData["ToastMessage"] = "Chưa chọn tuỳ chọn để thêm.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var ok = await _cartService.AddOptionsToCartItemAsync(accountId, cartItemId, optionIds, jwt);
                TempData["ToastType"] = ok ? "success" : "error";
                TempData["ToastMessage"] = ok ? "Đã thêm tuỳ chọn." : "Không thể thêm tuỳ chọn.";
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Lỗi khi thêm tuỳ chọn: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        // ===== BỎ 1 TUỲ CHỌN KHỎI CART ITEM =====
        // View có thể gửi optionId trực tiếp (khuyến nghị).
        // Nếu View hiện tại chỉ có cartItemOptionId thì mình map sang optionId bằng cách load lại cart items.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAddon(Guid cartItemId, Guid? optionId, Guid? cartItemOptionId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountID");
            var jwt = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            {
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Vui lòng đăng nhập để bỏ tuỳ chọn.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (cartItemId == Guid.Empty)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Thiếu CartItemId.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                Guid oid = optionId ?? Guid.Empty;

                // Nếu chưa có optionId mà chỉ có cartItemOptionId -> map sang optionId
                if (oid == Guid.Empty && cartItemOptionId.HasValue && cartItemOptionId.Value != Guid.Empty)
                {
                    var items = await _cartService.GetCartItemsByAccountIdAsync(accountId, jwt);
                    var hit = items
                        .FirstOrDefault(ci => ci.CartItemId == cartItemId)?
                        .Options?
                        .FirstOrDefault(o => o.CartItemOptionId == cartItemOptionId.Value);

                    if (hit != null)
                        oid = hit.OptionId;
                }

                if (oid == Guid.Empty)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = "Không xác định được tuỳ chọn cần xoá.";
                    return RedirectToAction(nameof(Index));
                }

                var ok = await _cartService.RemoveOptionFromCartItemAsync(accountId, cartItemId, oid, jwt);
                TempData["ToastType"] = ok ? "success" : "error";
                TempData["ToastMessage"] = ok ? "Đã bỏ tuỳ chọn." : "Không thể bỏ tuỳ chọn.";
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                TempData["ToastType"] = "warning";
                TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Lỗi khi bỏ tuỳ chọn: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyVoucher([FromBody] dynamic dto)
        {
            string code = dto?.code;
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { ok = false, message = "Thiếu mã" });

            // TODO: validate code qua service (thời hạn, lượt, …)
            // HttpContext.Session.SetString("APPLIED_VOUCHER_CODE", code);
            return Json(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveVoucher()
        {
            // HttpContext.Session.Remove("APPLIED_VOUCHER_CODE");
            return Json(new { ok = true });
        }

    }
}


