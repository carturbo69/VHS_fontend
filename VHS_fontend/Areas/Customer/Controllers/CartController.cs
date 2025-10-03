using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VHS_fontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        // ===== Models =====
        public class CartAddon
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public int Price { get; set; }                  // VND
            public string Type { get; set; } = "check";     // "check" | "qty"
            public int Qty { get; set; } = 0;               // check: 0/1; qty: >=0
            public int Subtotal => Price * Qty;
        }

        public class CartItem
        {
            public Guid Key { get; set; } = Guid.NewGuid();
            public int ServiceId { get; set; }
            public string Name { get; set; } = "";
            public int PriceValue { get; set; }             // giá dịch vụ chính
            public int Quantity { get; set; } = 1;          // luôn 1 gói
            public string ImageUrl { get; set; } = "";
            public List<CartAddon> AddOns { get; set; } = new();

            public int AddOnTotal => AddOns.Sum(a => a.Subtotal);
            public int LineTotal => PriceValue * Quantity + AddOnTotal;
        }

        // Tạm dùng static list
        private static readonly List<CartItem> _cart = new();

        // ===== View giỏ hàng =====
        [HttpGet]
        public IActionResult Index() => View(_cart);

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
            // chống null
            addonId ??= Array.Empty<int>();
            addonName ??= Array.Empty<string>();
            addonType ??= Array.Empty<string>();
            addonPrice ??= Array.Empty<int>();
            addonQty ??= Array.Empty<int>();

            var addons = new List<CartAddon>();
            for (int i = 0; i < addonId.Length; i++)
            {
                var qty = i < addonQty.Length ? addonQty[i] : 0;
                if (qty <= 0) continue;

                addons.Add(new CartAddon
                {
                    Id = addonId[i],
                    Name = i < addonName.Length ? addonName[i] : "",
                    Price = i < addonPrice.Length ? addonPrice[i] : 0,
                    Type = i < addonType.Length ? addonType[i] : "check",
                    Qty = qty
                });
            }

            _cart.Add(new CartItem
            {
                ServiceId = id,
                Name = name,
                PriceValue = priceValue,
                Quantity = Math.Max(1, quantity),   // bạn đang gửi 1
                ImageUrl = imageUrl ?? "",
                AddOns = addons
            });

            return RedirectToAction(nameof(Index));
        }

        // ===== Cập nhật / Xoá =====
        [HttpPost]
        public IActionResult UpdateQty(Guid key, int qty)
        {
            var it = _cart.FirstOrDefault(x => x.Key == key);
            if (it != null) it.Quantity = Math.Max(1, qty);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Remove(Guid key)
        {
            var it = _cart.FirstOrDefault(x => x.Key == key);
            if (it != null) _cart.Remove(it);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            _cart.Clear();
            return RedirectToAction(nameof(Index));
        }
    }
}
