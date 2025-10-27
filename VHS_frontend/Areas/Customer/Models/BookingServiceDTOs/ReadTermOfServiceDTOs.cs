namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class ReadTermOfServiceDTOs
    {
        public Guid ToSid { get; set; }
        public Guid ProviderId { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
