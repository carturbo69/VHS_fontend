namespace VHS_frontend.Areas.Provider.Models.Feedback
{
    public class ProviderFeedbackViewModel
    {
        public List<ServiceFeedbackGroup> ServiceFeedbacks { get; set; } = new();
        public FeedbackStats OverallStats { get; set; } = new();
    }

    public class ServiceFeedbackGroup
    {
        public Guid ServiceId { get; set; }               // Guid ?? kh?p BE
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceIcon { get; set; } = string.Empty; // URL tuy?t ??i t? API
        public double AverageRating { get; set; }
        public int TotalFeedbacks { get; set; }
        public List<CustomerFeedback> Feedbacks { get; set; } = new();
    }

    public class CustomerFeedback
    {
        public Guid ReviewId { get; set; }                // Guid ?? kh?p BE
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAvatar { get; set; } = string.Empty; // URL tuy?t ??i t? API
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }

        // BE tr? List<string> url ?nh => gi? nguyên ?? kh?i ph?i map
        public List<string> Images { get; set; } = new();

        // BE tr? reply là string? => gi? nguyên ?? kh?i ph?i map
        public string? Reply { get; set; }
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

    public class ProviderReplyRequestDto
    {
        public Guid ReviewId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
