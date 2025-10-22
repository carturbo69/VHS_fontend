using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.Notification
{
    public class NotificationDTO
    {
        public string NotificationId { get; set; } = string.Empty;
        public string AccountReceivedId { get; set; } = string.Empty;
        public string ReceiverRole { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedId { get; set; } // ID của booking, payment, etc.
    }

    public class NotificationListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<NotificationDTO> Data { get; set; } = new List<NotificationDTO>();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationDetailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationDTO Data { get; set; } = new NotificationDTO();
    }

    public class MarkReadRequest
    {
        [Required]
        public string NotificationId { get; set; } = string.Empty;
    }

    public class NotificationStats
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int TodayCount { get; set; }
    }
}
