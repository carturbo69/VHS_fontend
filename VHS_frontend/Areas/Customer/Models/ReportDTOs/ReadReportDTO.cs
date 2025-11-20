namespace VHS_frontend.Areas.Customer.Models.ReportDTOs
{
    public class ReadReportDTO
    {
        public Guid ComplaintId { get; set; }
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ProviderId { get; set; }
        public string ComplaintType { get; set; } = null!;
        public ReportTypeEnum ReportType { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public string[]? AttachmentUrls { get; set; }
        
        // Basic info for display
        public string? ProviderName { get; set; }
        public string? ServiceName { get; set; }
        public int DaysSinceCreated { get; set; }
        
        // ✅ Refund information (khi ComplaintType là "RefundRequest" hoặc "Yêu cầu hoàn tiền")
        public string? CancelReason { get; set; }
        public string? RefundStatus { get; set; } // "Pending", "Approved", "Rejected", "NoRefund"
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
        public decimal? RefundAmount { get; set; }
    }
}





