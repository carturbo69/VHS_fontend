using System.Text.Json.Serialization;

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
        [JsonPropertyName("checkerRecords")]
        public List<BookingCheckerDTO> CheckerRecords { get; set; } = new();

        // Auto Cancel Info
        public int? AutoCancelMinutes { get; set; } // Thời gian tự động hủy riêng cho booking này (phút)

        // Timeline / Tracking Events
        [JsonPropertyName("timeline")]
        public List<TrackingEventDTO> Timeline { get; set; } = new();
    }

    public class TrackingEventDTO
    {
        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }
        
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty; // CREATED/CONFIRMED/...
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("proofs")]
        public List<MediaProofDTO> Proofs { get; set; } = new();
    }

    public class MediaProofDTO
    {
        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = "image"; // hoặc enum
        
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        
        [JsonPropertyName("caption")]
        public string? Caption { get; set; }
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
        [JsonPropertyName("checkerId")]
        public Guid CheckerId { get; set; }
        
        [JsonPropertyName("forStatus")]
        public string ForStatus { get; set; } = string.Empty;
        
        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = string.Empty;
        
        [JsonPropertyName("fileUrl")]
        public string FileUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("uploadedAt")]
        public DateTime UploadedAt { get; set; }
        
        [JsonPropertyName("staffName")]
        public string? StaffName { get; set; }
    }
}

