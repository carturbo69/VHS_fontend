namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    public class HistoryBookingDetailDTO
    {
        // ===== Booking / Order =====
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;

        // DÙNG CHUỖI
        public string Status { get; set; } = "Pending";

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        // Người nhận & địa chỉ
        public string RecipientFullName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string? ShippingTrackingCode { get; set; }

        // Nhà cung cấp / nhân sự thực hiện
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public Guid? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StaffImage { get; set; }

        // ===== Service =====
        public ServiceInBookingDTO Service { get; set; } = new();
        public List<OptionDTO> Options { get; set; } = new();

        // ===== Giá tiền =====
        public decimal ShippingFee { get; set; }
        public decimal VoucherDiscount { get; set; }
        public string PaymentMethod { get; set; } = " ";

        [JsonIgnore] public decimal Subtotal => Service.LineTotal;
        [JsonIgnore] public decimal Total => Subtotal + ShippingFee - VoucherDiscount;

        public decimal PaidAmount { get; set; }

        public bool HasReview { get; set; } = false;   // NEW

        // ===== Tiến trình / Tracking =====
        public List<TrackingEventDTO> Timeline { get; set; } = new();

        // ===== Helper hiển thị =====
        [JsonIgnore]
        public string NormalizedStatus
        {
            get
            {
                var s = (Status ?? "").Trim();
                if (s.Equals("Pending", StringComparison.OrdinalIgnoreCase)) return "Pending";
                if (s.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)) return "Confirmed";
                if (s.Equals("InProgress", StringComparison.OrdinalIgnoreCase)) return "InProgress";
                if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Completed";
                if (s.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
                if (s.Equals("All", StringComparison.OrdinalIgnoreCase)) return "All";
                // fallback: giữ nguyên
                return s;
            }
        }

        [JsonIgnore]
        public string StatusVi => NormalizedStatus switch
        {
            "Pending" => "Chờ xác nhận",
            "Confirmed" => "Xác Nhận",
            "InProgress" => "Bắt Đầu Làm Việc",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            "All" => "Tất cả",
            _ => "—"
        };

        // ====== Sub DTOs ======
        public class ServiceInBookingDTO
        {
            public Guid ServiceId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Image { get; set; } = "/images/placeholder.png";
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; } = 1;
            public string UnitType { get; set; } = "đ";
            public List<OptionDTO> Options { get; set; } = new();
            public bool IncludeOptionPriceToLineTotal { get; set; } = false;

            public decimal OptionsTotal => Options?.Sum(o => o.Price) ?? 0m;
            public decimal LineTotal => (UnitPrice * Quantity) + (IncludeOptionPriceToLineTotal ? OptionsTotal : 0m);
        }

        public class TrackingEventDTO
        {
            public DateTimeOffset Time { get; set; }
            public string Code { get; set; } = string.Empty; // CREATED/CONFIRMED/...
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<MediaProofDTO> Proofs { get; set; } = new();
        }

        public class MediaProofDTO
        {
            public string MediaType { get; set; } = "image"; // hoặc enum
            public string Url { get; set; } = string.Empty;
            public string? Caption { get; set; }
        }
    }
}
