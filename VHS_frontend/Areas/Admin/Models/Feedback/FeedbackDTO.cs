using System;

namespace VHS_frontend.Areas.Admin.Models.Feedback
{
    public class FeedbackDTO
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public Guid? ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public Guid? ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public string Status => IsDeleted ? "Đã xóa" : IsVisible ? "Hiển thị" : "Đã ẩn";
    }
}
