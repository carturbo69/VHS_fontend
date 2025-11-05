namespace VHS_frontend.Areas.Admin.Models.Booking
{
    /// <summary>
    /// Filter DTO cho Admin Booking - Lấy TẤT CẢ bookings của TẤT CẢ providers
    /// (KHÔNG có ProviderId - khác với ProviderBookingFilterDTO)
    /// </summary>
    public class AdminBookingFilterDTO
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        // KHÔNG có ProviderId - để lấy tất cả bookings từ tất cả providers
    }
}

