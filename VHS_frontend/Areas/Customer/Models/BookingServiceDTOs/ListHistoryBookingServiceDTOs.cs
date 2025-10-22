namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class ListHistoryBookingServiceDTOs
    {
        public List<BookingServiceItemDTO> Items { get; set; } = new();
    }

    public class OptionDTO
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = null!;
    }

    public class BookingServiceItemDTO
    {
        // Booking
        public Guid BookingId { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; } = null!;
        public string Address { get; set; } = null!;

        // Provider
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = null!;

        // Service (sản phẩm)
        public Guid ServiceId { get; set; }
        public string ServiceTitle { get; set; } = null!;
        public decimal ServicePrice { get; set; }
        public string ServiceUnitType { get; set; } = null!;
        public string? ServiceImages { get; set; } // giữ dạng string (JSON/comma) như DB

        // Options gắn với booking
        public List<OptionDTO> Options { get; set; } = new();

        // (tuỳ chọn) Tổng chi phí = giá service + tổng giá option
        public decimal TotalPrice => ServicePrice + (Options?.Sum(o => o.Price) ?? 0m);


    }
}
