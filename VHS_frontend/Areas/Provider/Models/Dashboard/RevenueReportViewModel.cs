namespace VHS_frontend.Areas.Provider.Models.Dashboard
{
    public class RevenueReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCompletedBookings { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal? PreviousRevenue { get; set; }
        public decimal? RevenueGrowthPercent { get; set; }
        public List<TopServiceRevenueViewModel> TopServices { get; set; } = new();
        public List<RevenueDetailItemViewModel> Details { get; set; } = new();
        public List<DailyRevenueViewModel> DailyRevenues { get; set; } = new();
    }

    public class TopServiceRevenueViewModel
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public int BookingCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RevenueDetailItemViewModel
    {
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public DateTime CompletedDate { get; set; }
        public string CustomerName { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class RevenueReportFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }
}

