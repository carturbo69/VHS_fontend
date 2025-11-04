namespace VHS_frontend.Areas.Provider.Models.Booking
{
    public class BookingStatisticsDTO
    {
        public int TotalBookings { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CanceledCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ThisMonthBookings { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public int TotalServices { get; set; }
    }
}

