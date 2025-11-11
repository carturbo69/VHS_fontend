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
        public string? ProviderImages { get; set; } // Logo/ảnh của provider
        public Guid? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StaffImage { get; set; }
        // Optional contact fields (may be null if backend doesn't supply)
        public string? StaffPhone { get; set; }
        public string? StaffAddress { get; set; }

        // ===== Service =====
        public ServiceInBookingDTO Service { get; set; } = new();
        public string? ServiceImages { get; set; } // giữ dạng string (JSON/comma) như DB
        public List<OptionDTO> Options { get; set; } = new();

        // ===== Giá tiền =====
        public decimal ShippingFee { get; set; }
        public decimal VoucherDiscount { get; set; }
        public string PaymentMethod { get; set; } = " ";
        public string? PaymentStatus { get; set; } // Trạng thái thanh toán từ Payment.Status

        [JsonIgnore] public decimal Subtotal => Service.LineTotal;
        [JsonIgnore] public decimal Total => Subtotal + ShippingFee - VoucherDiscount;

        public decimal PaidAmount { get; set; }

        public bool HasReview { get; set; } = false;   // NEW

        // ===== Refund Info =====
        public string? CancelReason { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? RefundStatus { get; set; } // Status của refund request: "Pending", "Approved", "Rejected"
        public string? ResolutionNote { get; set; } // Ghi chú từ admin khi approve/reject

        // ===== Tiến trình / Tracking =====
        public List<TrackingEventDTO> Timeline { get; set; } = new();

        // ===== Helper hiển thị =====
        [JsonIgnore]
        public string NormalizedStatus
        {
            get
            {
                var s = (Status ?? "").Trim();
                // Tiếng Anh
                if (s.Equals("Pending", StringComparison.OrdinalIgnoreCase)) return "Pending";
                if (s.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)) return "Confirmed";
                if (s.Equals("InProgress", StringComparison.OrdinalIgnoreCase)) return "InProgress";
                if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Completed";
                if (s.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
                if (s.Equals("All", StringComparison.OrdinalIgnoreCase)) return "All";
                // Tiếng Việt -> chuẩn hóa sang tiếng Anh
                if (s.Equals("Chờ xác nhận", StringComparison.OrdinalIgnoreCase)) return "Pending";
                if (s.Equals("Đang chờ xử lý", StringComparison.OrdinalIgnoreCase)) return "Pending";
                if (s.Equals("Chờ xử lý", StringComparison.OrdinalIgnoreCase)) return "Pending";
                if (s.Equals("Xác Nhận", StringComparison.OrdinalIgnoreCase)) return "Confirmed";
                if (s.Equals("Bắt Đầu Làm Việc", StringComparison.OrdinalIgnoreCase)) return "InProgress";
                if (s.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase)) return "Completed";
                if (s.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
                if (s.Equals("Tất cả", StringComparison.OrdinalIgnoreCase)) return "All";
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

        /// <summary>
        /// Kiểm tra xem booking có đang ở trạng thái Pending (cả tiếng Anh và tiếng Việt) không.
        /// Cho phép hủy đơn ở các trạng thái này.
        /// </summary>
        [JsonIgnore]
        public bool IsPendingStatus
        {
            get
            {
                var status = (Status ?? "").Trim();
                return status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                       status.Equals("Chờ xác nhận", StringComparison.OrdinalIgnoreCase) ||
                       status.Equals("Đang chờ xử lý", StringComparison.OrdinalIgnoreCase) ||
                       status.Equals("Chờ xử lý", StringComparison.OrdinalIgnoreCase);
            }
        }

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
