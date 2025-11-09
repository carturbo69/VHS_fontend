using System;
using System.Collections.Generic;
using System.Linq;

namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class BookingViewModel
    {
        // Người nhận
        public string RecipientFullName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;

        // Địa chỉ hiện chọn (hiển thị)
        public UserAddressDto Address { get; set; } = new UserAddressDto();

        // Cho modal thay đổi
        public List<UserAddressDto> Addresses { get; set; } = new();
        public Guid? SelectedAddressId { get; set; }

        // Chuỗi địa chỉ snapshot sẽ post khi PlaceOrder
        public string AddressText { get; set; } = "";

        // Giỏ hàng
        public List<BookItem> Items { get; set; } = new();

        // Voucher
        //public string? VoucherCode { get; set; }
        public Guid? VoucherId { get; set; }
        public decimal VoucherDiscount { get; set; }

        // 👉 Thêm để client hiển thị breakdown ngay khi vào trang
        public decimal VoucherPercent { get; set; }                 // % giảm (nếu có)
        public decimal VoucherMaxAmount { get; set; }           // trần giảm (nếu có)

        // Thanh toán
        public List<PaymentMethod> PaymentMethods { get; set; } = new();

        // KHÔNG auto-chọn để ép người dùng chọn trong UI
        public string? SelectedPaymentCode { get; set; } = null;

        // Tổng tiền (set từ controller sau khi tính)
        public decimal Subtotal { get; set; }                   // Tổng tiền hàng (items + options)
        public decimal Total { get; set; }                      // = Subtotal - VoucherDiscount (và >= 0 ở controller)

        // ======= Helpers / Samples =======
        public static List<UserAddressDto> AddressSample()
        {
            var address1 = new UserAddressDto
            {
                AddressId = Guid.NewGuid(),
                ProvinceName = "Cần Thơ",
                DistrictName = "Ninh Kiều",
                WardName = "Hưng Lợi",
                StreetAddress = "118/22, Hẻm 107 Đường 3/2, Ngang Quán Nhậu Tư Minh"
            };

            var address2 = new UserAddressDto
            {
                AddressId = Guid.NewGuid(),
                ProvinceName = "Hồ Chí Minh",
                DistrictName = "Quận 1",
                WardName = "Bến Nghé",
                StreetAddress = "12 Lê Duẩn"
            };

            return new List<UserAddressDto> { address1, address2 };
        }
    }

    public class BookItem
    {
        public Guid CartItemId { get; set; } = Guid.NewGuid();
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }
        public string Provider { get; set; } = "Khác";
        public string ServiceName { get; set; } = string.Empty;
        public string Image { get; set; } = "/images/placeholder.png";
        public string? ServiceImages { get; set; } // JSON/comma-separated string
        public string? ProviderImages { get; set; } // JSON/comma-separated string for provider logo
        public decimal UnitPrice { get; set; }
        public DateTime BookingTime { get; set; } = DateTime.Now;

        public List<BookItemOption> Options { get; set; } = new();

        // ✅ thêm: chỉ những OptionIds hiển thị trên trang này sẽ được post về
        public List<Guid> OptionIds { get; set; } = new();

        public decimal OptionsTotal => Options?.Sum(o => o.Price) ?? 0m;

        public decimal LineTotal => UnitPrice + OptionsTotal;
    }

    public class BookItemOption
    {
        public Guid OptionId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }

    public class PaymentMethod
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class TermOfServiceDto
    {
        public Guid ToSid { get; set; }
        public Guid ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
