using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.ReportDTOs
{
    public class CreateReportDTO
    {
        [Required(ErrorMessage = "Booking ID is required")]
        public Guid BookingId { get; set; }

        [Required(ErrorMessage = "Report type is required")]
        public ReportTypeEnum ReportType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public Guid? ProviderId { get; set; }

        // File attachments support
        public List<IFormFile>? Attachments { get; set; }
    }

    public enum ReportTypeEnum
    {
        ServiceQuality,      // Chất lượng dịch vụ
        ProviderMisconduct,  // Hành vi sai trái của provider
        StaffMisconduct,     // Hành vi sai trái của nhân viên
        Dispute,            // Tranh chấp
        TechnicalIssue,     // Vấn đề kỹ thuật
        Other               // Khác
    }

    public enum ReportStatusEnum
    {
        Pending,      // Pending Review
        InReview,     // Being reviewed by admin
        Resolved,     // Resolved
        Rejected      // Rejected
    }
}



