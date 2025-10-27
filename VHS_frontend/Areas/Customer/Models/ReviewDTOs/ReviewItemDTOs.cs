namespace VHS_frontend.Areas.Customer.Models.ReviewDTOs
{
    public class ReadReviewDTOs
    {
        public Guid ReviewId { get; set; }

        public Guid ServiceId { get; set; }

        public Guid UserId { get; set; }

        public int? Rating { get; set; }

        public string? Comment { get; set; }

        public List<string> Images { get; set; } = new();

        public string? Reply { get; set; }

        public DateTime? CreatedAt { get; set; }

        public bool? IsDeleted { get; set; }

        public string? FullName { get; set; }
    }
}
