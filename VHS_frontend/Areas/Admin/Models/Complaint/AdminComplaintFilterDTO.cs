namespace VHS_frontend.Areas.Admin.Models.Complaint
{
    public class AdminComplaintFilterDTO
    {
        public string? Status { get; set; }
        public string? Type { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "Desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}


