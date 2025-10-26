namespace VHS_frontend.Areas.Provider.Models.Booking
{
    public class BookingDetailDTO
    {
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;

        // Customer Info
        public Guid UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Service Info
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public string? ServiceImages { get; set; }

        // Booking Info
        public DateTime BookingTime { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CancelReason { get; set; }  // ✨ Lý do hủy đơn

        // Options
        public List<BookingOptionDetailDTO> Options { get; set; } = new();

        // Payment Info
        public Guid? PaymentId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }

        // Staff Info
        public Guid? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StaffPhone { get; set; }

        // Voucher Info
        public Guid? VoucherId { get; set; }
        public string? VoucherCode { get; set; }
        public decimal? DiscountAmount { get; set; }

        // Checker Records
        public List<BookingCheckerDTO> CheckerRecords { get; set; } = new();
    }

    public class BookingOptionDetailDTO
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }

    public class BookingCheckerDTO
    {
        public Guid CheckerId { get; set; }
        public string ForStatus { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? StaffName { get; set; }
    }
}

