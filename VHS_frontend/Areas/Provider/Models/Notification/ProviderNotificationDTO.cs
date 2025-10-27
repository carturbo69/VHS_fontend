using System;

namespace VHS_frontend.Areas.Provider.Models.Notification
{
    public class ProviderNotificationDTO
    {
        public Guid NotificationId { get; set; }
        public Guid AccountReceivedId { get; set; }
        public string ReceiverRole { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverEmail { get; set; }
    }
}

