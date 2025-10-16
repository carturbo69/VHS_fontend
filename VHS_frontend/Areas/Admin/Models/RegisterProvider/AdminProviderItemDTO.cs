namespace VHS_frontend.Areas.Admin.Models.RegisterProvider
{
    public class AdminProviderItemDTO
    {
        public Guid ProviderId { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
