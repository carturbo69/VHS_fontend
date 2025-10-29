namespace VHS_frontend.Areas.Admin.Models.Complaint
{
    public class ComplaintDetailsDTO : AdminComplaintDTO
    {
        public string? BookingStatus { get; set; }
        public DateTime? BookingDate { get; set; }
        public string? ServiceLocation { get; set; }
        public string? ProviderEmail { get; set; }
        public string? ProviderPhone { get; set; }
        public string? CustomerPhone { get; set; }
    }

    public class HandleComplaintDTO
    {
        public string Status { get; set; } = null!;
        public string? ResolutionNote { get; set; }
    }
}





