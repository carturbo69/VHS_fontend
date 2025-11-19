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
        public Guid? TagId { get; set; }
        public string Type { get; set; } = null!; // enum: checkbox, radio, text, optional, etc.
        public Guid? Family { get; set; } // For radio buttons: if one is selected, others are hidden
        public string? Value { get; set; } // Stores the value if any
    }

    public class BookingServiceItemDTO
    {
        // Booking
        public Guid BookingId { get; set; }
        public DateTime BookingTime { get; set; }
        public DateTime? CreatedAt { get; set; } // Thời gian tạo booking
        public string Status { get; set; } = null!;
        public string Address { get; set; } = null!;

        // Provider
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = null!;
        public string? ProviderImages { get; set; } // Logo/ảnh của provider

        // Service (sản phẩm)
        public Guid ServiceId { get; set; }
        public string ServiceTitle { get; set; } = null!;
        public decimal ServicePrice { get; set; }
        public string ServiceUnitType { get; set; } = null!;
        public string? ServiceImages { get; set; } // giữ dạng string (JSON/comma) như DB

        // Options gắn với booking
        public List<OptionDTO> Options { get; set; } = new();

        // NEW: số tiền voucher áp dụng cho booking này (nếu có). Backend nên trả, mặc định 0.
        public decimal VoucherDiscount { get; set; } = 0m;

        public bool HasReview { get; set; } = false;

        // Payment info
        public decimal? PaidAmount { get; set; }
        public string? PaymentStatus { get; set; }

        // (tuỳ chọn) Tổng chi phí = giá service (Options no longer have Price)
        public decimal TotalPrice => ServicePrice;


    }
}
