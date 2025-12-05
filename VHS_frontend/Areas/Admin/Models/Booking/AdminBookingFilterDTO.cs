namespace VHS_frontend.Areas.Admin.Models.Booking
{
    /// <summary>
    /// Filter DTO cho Admin Booking - Lấy TẤT CẢ bookings của TẤT CẢ providers
    /// (ProviderId là optional - nếu có sẽ filter theo provider)
    /// </summary>
    public class AdminBookingFilterDTO
    {
        public Guid? ProviderId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

