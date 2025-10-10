namespace VHS_frontend.Models.ReviewDTOs
{
    public class ReadReviewDTOs
    {
        public Guid ReviewId { get; set; }

        public Guid ServiceId { get; set; }

        public Guid UserId { get; set; }

        public int? Rating { get; set; }

        public string? Comment { get; set; }

        public string? Images { get; set; }

        public string? Reply { get; set; }

        public DateTime? CreatedAt { get; set; }

        public bool? IsDeleted { get; set; }

        public string? FullName { get; set; }
    }
}
