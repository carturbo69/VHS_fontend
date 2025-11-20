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
        public DateTime? ConfirmedAt { get; set; }  // Thời gian xác nhận đơn hàng
        public string? CancelReason { get; set; }  // Lý do hủy đơn

        // Address Details từ UserAddresses
        public Guid? AddressId { get; set; }
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public string? StreetAddress { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }

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

        // Auto Cancel Info
        public int? AutoCancelMinutes { get; set; } // Thời gian tự động hủy riêng cho booking này (phút)
    }

    public class BookingOptionDetailDTO
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public Guid? TagId { get; set; }
        public string Type { get; set; } = string.Empty; // enum: checkbox, radio, text, optional, etc.
        public Guid? Family { get; set; } // For radio buttons: if one is selected, others are hidden
        public string? Value { get; set; } // Stores the value if any
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

