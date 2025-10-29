namespace VHS_frontend.Areas.Customer.Models.ReportDTOs
{
    public class ReportFilterDTO
    {
        public ReportTypeEnum? ReportType { get; set; }
        public ReportStatusEnum? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }

    public class PaginatedReportDTO
    {
        public List<ReadReportDTO> Reports { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}


