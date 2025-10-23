namespace VHS_frontend.Areas.Provider.Models.Feedback
{
    public class ProviderFeedbackViewModel
    {
        public List<ServiceFeedbackGroup> ServiceFeedbacks { get; set; } = new();
        public FeedbackStats OverallStats { get; set; } = new();
    }

    public class ServiceFeedbackGroup
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceIcon { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalFeedbacks { get; set; }
        public List<CustomerFeedback> Feedbacks { get; set; } = new();
    }

    public class CustomerFeedback
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAvatar { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }
        public List<FeedbackImage> Images { get; set; } = new();
        public ProviderReply? Reply { get; set; }
    }

    public class FeedbackImage
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }

    public class ProviderReply
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ProviderName { get; set; } = string.Empty;
    }

    public class FeedbackStats
    {
        public int TotalFeedbacks { get; set; }
        public double AverageRating { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }
}
