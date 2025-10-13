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


        //public async Task<IActionResult> Index()
        //{
        //    // Lấy AccountID & Token từ Session (đã set sau khi Login)
        //    var accountIdStr = HttpContext.Session.GetString("AccountID");
        //    var jwt = HttpContext.Session.GetString("JWToken");

        //    if (string.IsNullOrWhiteSpace(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
        //    {
        //        TempData["ToastType"] = "warning";
        //        TempData["ToastMessage"] = "Vui lòng đăng nhập để xem giỏ hàng.";
        //        return RedirectToAction("Login", "Account", new { area = "" });
        //    }

        //    try
        //    {
        //        var items = await _cartService.GetCartItemsByAccountIdAsync(accountId, jwt);
        //        return View(items); // Model: List<ReadCartItemDTOs>
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        // Hết hạn token -> bắt đăng nhập lại
        //        HttpContext.Session.Clear();
        //        TempData["ToastType"] = "warning";
        //        TempData["ToastMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
        //        return RedirectToAction("Login", "Account", new { area = "" });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Lỗi khác
        //        TempData["ToastType"] = "error";
        //        TempData["ToastMessage"] = $"Không thể tải giỏ hàng: {ex.Message}";
        //        return View(new System.Collections.Generic.List<ReadCartItemDTOs>());
        //    }
        //}

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


        // ===== Thêm vào giỏ (DUY NHẤT action này, POST) =====
        [HttpPost]
        public IActionResult AddToCart(
            int id,
            string name,
            int priceValue,
            int quantity,
            string? imageUrl,
            int[]? addonId,
            string[]? addonName,
            string[]? addonType,   // "check" | "qty"
            int[]? addonPrice,
            int[]? addonQty
        )
        {
          
            return RedirectToAction(nameof(Index));
        }

        // ===== Cập nhật / Xoá =====
        [HttpPost]
        public IActionResult UpdateQty(Guid key, int qty)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Remove(Guid key)
        {
          
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            
            return RedirectToAction(nameof(Index));
        }
    }
}


