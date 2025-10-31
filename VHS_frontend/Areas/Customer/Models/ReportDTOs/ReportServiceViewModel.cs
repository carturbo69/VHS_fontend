namespace VHS_frontend.Areas.Customer.Models.ReportDTOs
{
    public class ReportServiceViewModel
    {
        public Guid BookingId { get; set; }
        public Guid? ProviderId { get; set; }
        
        // Service information for display
        public string ServiceTitle { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceImage { get; set; } = "/images/VeSinh.jpg";
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }

        // Report form data
        public Dictionary<ReportTypeEnum, string> ReportTypes { get; set; } = new();
    }
}



