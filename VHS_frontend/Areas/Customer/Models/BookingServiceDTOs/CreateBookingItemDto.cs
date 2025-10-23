namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class CreateManyBookingsDto
    {
        public Guid AccountId { get; set; }
        public string Address { get; set; } = null!;
        public Guid? VoucherId { get; set; }   // 🎟️ thêm trường này
        public List<CreateBookingItemDto> Items { get; set; } = new();
    }

    public class CreateBookingItemDto
    {
        public Guid ServiceId { get; set; }
        public DateTime BookingTime { get; set; }   // 🕒 đã có
        public List<Guid> OptionIds { get; set; } = new();
    }

    // API_Backend.DTOs.BookingServiceDTOs
    public class CreateManyBookingsResult
    {
        public List<Guid> BookingIds { get; set; } = new();
        public List<BookingAmountItem> Breakdown { get; set; } = new(); // 👈 thêm
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    public class BookingAmountItem
    {
        public Guid BookingId { get; set; }
        public decimal Subtotal { get; set; }   // giá dịch vụ + options (chưa trừ voucher)
        public decimal Discount { get; set; }   // phần voucher phân bổ
        public decimal Amount { get; set; }     // Subtotal - Discount (>= 0)
    }

}
