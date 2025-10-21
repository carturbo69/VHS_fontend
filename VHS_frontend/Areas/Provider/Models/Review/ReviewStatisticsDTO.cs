namespace VHS_frontend.Areas.Provider.Models.Review
{
    public class ReviewStatisticsDTO
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalReplied { get; set; }
        public int TotalNotReplied { get; set; }
    }
}

