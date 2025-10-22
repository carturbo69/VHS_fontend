namespace VHS_frontend.Areas.Admin.Models.Notification
{
    public class AdminNotificationQuery
    {
        public string? Keyword { get; set; }
        public string? Role { get; set; }
        public string? NotificationType { get; set; }
        public bool? IsRead { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
