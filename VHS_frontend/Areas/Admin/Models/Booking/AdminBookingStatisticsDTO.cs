namespace VHS_frontend.Areas.Admin.Models.Booking
{
    public class AdminBookingStatisticsDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueChartDataDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrdersByHourDTO
    {
        public string Period { get; set; } = "";
        public int Orders { get; set; }
    }
}

