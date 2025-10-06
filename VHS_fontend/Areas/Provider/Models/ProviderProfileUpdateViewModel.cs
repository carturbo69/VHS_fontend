namespace VHS_frontend.Areas.Provider.Models
{
    // Dùng cho form update (PUT)
    public class ProviderProfileUpdateViewModel
    {
        public string ProviderName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Images { get; set; }
    }
}
