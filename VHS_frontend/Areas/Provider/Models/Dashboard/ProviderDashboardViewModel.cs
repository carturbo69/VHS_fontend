using VHS_frontend.Areas.Provider.Models.Schedule;

namespace VHS_frontend.Areas.Provider.Models.Dashboard
{
    public class ProviderDashboardViewModel
    {
        public string ProviderName { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingBookings { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
        public MonthlyStatsViewModel MonthlyStats { get; set; } = new();
        public ScheduleOverviewViewModel? ScheduleOverview { get; set; }
    }

    public class RecentOrderViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class MonthlyStatsViewModel
    {
        public decimal January { get; set; }
        public decimal February { get; set; }
        public decimal March { get; set; }
        public decimal April { get; set; }
        public decimal May { get; set; }
        public decimal June { get; set; }
        public decimal July { get; set; }
        public decimal August { get; set; }
        public decimal September { get; set; }
        public decimal October { get; set; }
        public decimal November { get; set; }
        public decimal December { get; set; }
    }
}


