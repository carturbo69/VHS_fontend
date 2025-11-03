namespace VHS_frontend.Areas.Admin.Models.Complaint
{
    public class AdminComplaintDTO
    {
        public Guid ComplaintId { get; set; }
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ProviderId { get; set; }
        public string ComplaintType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public string[]? AttachmentUrls { get; set; }
        
        // Basic info for admin
        public string? ProviderName { get; set; }
        public string? ServiceName { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public int DaysSinceCreated { get; set; }
    }

    public class PaginatedAdminComplaintDTO
    {
        public List<AdminComplaintDTO> Complaints { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    public class ComplaintStatisticsDTO
    {
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InReviewComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int RejectedComplaints { get; set; }
        public int HighPriorityComplaints { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public Dictionary<string, int> ComplaintsByType { get; set; } = new();
        public Dictionary<string, int> ComplaintsByStatus { get; set; } = new();
    }
}






