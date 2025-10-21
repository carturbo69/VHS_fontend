namespace VHS_frontend.Models.ServiceDTOs
{
    public class ProviderSummaryDTOs
    {
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? Images { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? JoinedAt { get; set; }

        public int TotalServices { get; set; }
        public double AverageRatingAllServices { get; set; }
    }
}
