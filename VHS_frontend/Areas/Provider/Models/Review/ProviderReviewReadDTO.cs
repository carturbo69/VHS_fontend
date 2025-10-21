namespace VHS_frontend.Areas.Provider.Models.Review
{
    public class ProviderReviewReadDTO
    {
        public Guid ReviewId { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserImage { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public string? Images { get; set; }
        public string? Reply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool HasReplied { get; set; }
    }
}

