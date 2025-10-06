namespace VHS_frontend.Areas.Provider.Models
{
    public class ProviderProfileReadViewModel
    {
        public Guid ProviderId { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Images { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
