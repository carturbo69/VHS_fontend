using System;

namespace VHS_frontend.Areas.Admin.Models.Notification
{
    public class AdminNotificationDTO
    {
        public Guid NotificationId { get; set; }
        public Guid? AccountReceivedId { get; set; }
        public string ReceiverRole { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverEmail { get; set; }
    }
}
