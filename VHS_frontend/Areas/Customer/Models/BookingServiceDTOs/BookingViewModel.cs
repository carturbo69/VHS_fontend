using System;
using System.Collections.Generic;
using System.Linq;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;

namespace VHS_frontend.Areas.Customer.Models.Booking
{
    public class BookingViewModel
    {
        // Người nhận
        public string RecipientFullName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;

        // Địa chỉ hiện chọn (dùng DTO này)
        public UserAddressDto Address { get; set; } = new UserAddressDto();

        // Cho modal thay đổi
        public List<UserAddressDto> Addresses { get; set; } = new();
        public Guid? SelectedAddressId { get; set; }

        public List<BookItem> Items { get; set; } = new();
        public string? VoucherCode { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public string SelectedPaymentCode { get; set; } = "BANK_TRANSFER";

        public decimal Subtotal => Items.Sum(i => i.LineTotal);
        public decimal Total => Subtotal + ShippingFee - VoucherDiscount;

        // ---- Sample data để test ----
        public static BookingViewModel Sample()
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

      

            var vm = new BookingViewModel
            {
                RecipientFullName = "Trần Hoài Anh",
                RecipientPhone = "+84 977 817 277",
                Address = address1,
                Addresses = new List<UserAddressDto> { address1, address2 },
                ShippingFee = 51100,
                //VoucherCode = "HELLO15K",
                //VoucherDiscount = 15540,
                PaymentMethods = new List<PaymentMethod>
                {
                    new PaymentMethod { Code = "EWALLET", DisplayName = "Ví ShopeePay" },
                    new PaymentMethod { Code = "CARD", DisplayName = "Thẻ Tín dụng/Ghi nợ" },
                    new PaymentMethod { Code = "GOOGLE_PAY", DisplayName = "Google Pay" },
                    new PaymentMethod { Code = "NAPAS", DisplayName = "Thẻ nội địa NAPAS" },
                    new PaymentMethod { Code = "COD", DisplayName = "Thanh toán khi nhận hàng" },
                    new PaymentMethod { Code = "BANK_TRANSFER", DisplayName = "Chuyển khoản ngân hàng" }
                }
            };

            // BookingViewModel.Sample()

            // ✅ Mỗi nhà cung cấp một ProviderId cố định (demo)
            var pidZiaja = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var pidGift = Guid.Parse("22222222-2222-2222-2222-222222222222");

            vm.Items.Add(new BookItem
            {
                ProviderId = pidZiaja,                 // ⬅️ THÊM
                Provider = "Intimate Ziaja Store",
                ServiceId = Guid.NewGuid(),
                ServiceName = "Dung Dịch Vệ Sinh Intimate With Lactic Acid Ziaja 200ml",
                Image = "/images/VeSinh.jpg",
                UnitPrice = 259000,
                BookingTime = DateTime.Now,
                Options = new List<BookItemOption>
    {
        new() { Name = "Bảo hiểm bảo vệ người tiêu dùng", Price = 2999,  Description = "Bảo hiểm trong quá trình sử dụng." },
        new() { Name = "Gói quà tặng cao cấp",            Price = 15000, Description = "Gói quà sang trọng thích hợp làm quà tặng." },
        new() { Name = "Giao hàng hỏa tốc",               Price = 30000, Description = "Giao trong ngày cho khu vực nội thành." },
        new() { Name = "Phiếu giảm giá lần sau",          Price = 0,     Description = "Tặng kèm phiếu giảm giá 10% cho đơn hàng kế tiếp." }
    }
            });

            vm.Items.Add(new BookItem
            {
                ProviderId = pidGift,                  // ⬅️ THÊM
                Provider = "(GIFT) Quà Tặng Ziaja",
                ServiceId = Guid.NewGuid(),
                ServiceName = "Quà tặng khách hàng",
                Image = "/images/VeSinh.jpg",
                UnitPrice = 0,
                BookingTime = DateTime.Now
            });


            return vm;
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
        public decimal UnitPrice { get; set; }
        public DateTime BookingTime { get; set; } = DateTime.Now;
        public List<BookItemOption> Options { get; set; } = new();
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
        public string? Url { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
