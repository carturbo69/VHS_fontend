namespace VHS_frontend.Areas.Customer.Models.RegisterProvider
{
    public class MyProviderDTO
    {
        public Guid ProviderId { get; set; }
        public string Status { get; set; } = ""; // Pending / Approved / Rejected ...
    }
}
