using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;

namespace VHS_frontend.Areas.Customer.Models.ReportDTOs
{
    public class ReportServiceViewModel
    {
        public Guid BookingId { get; set; }
        public Guid? ProviderId { get; set; }
        
        // Service information for display
        public string ServiceTitle { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderImages { get; set; } // Logo/ảnh của provider
        public string ServiceImage { get; set; } = "/images/VeSinh.jpg";
        public string? ServiceImages { get; set; } // giữ dạng string (JSON/comma) như DB
        
        // Pricing information (giống ListHistoryBooking)
        public decimal ServicePrice { get; set; } // Giá dịch vụ gốc
        public decimal OptionTotal { get; set; } // Tổng giá tùy chọn
        public decimal SubTotal { get; set; } // Tổng cộng (ServicePrice + OptionTotal)
        public decimal VoucherDiscount { get; set; } // Giảm giá
        public decimal GrandTotal { get; set; } // Thành tiền (SubTotal - VoucherDiscount)
        public decimal PaidAmount { get; set; } // Số tiền đã thanh toán
        public string PaymentStatus { get; set; } = string.Empty; // Trạng thái thanh toán
        
        // Options
        public List<OptionDTO> Options { get; set; } = new();
        
        // Legacy properties (giữ để tương thích)
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }

        // Report form data
        public Dictionary<ReportTypeEnum, string> ReportTypes { get; set; } = new();
    }
}





