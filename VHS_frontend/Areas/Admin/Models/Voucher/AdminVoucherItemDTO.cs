namespace VHS_frontend.Areas.Admin.Models.Voucher
{
    public class AdminVoucherItemDTO
    {
        public Guid VoucherId { get; set; }
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountMaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsedCount { get; set; }
        public bool? IsActive { get; set; }
        // Convenience flags
        public bool IsExpired =>
            EndDate.HasValue && EndDate.Value.Date < DateTime.UtcNow.Date;
        public bool IsUpcoming =>
            StartDate.HasValue && StartDate.Value.Date > DateTime.UtcNow.Date;
    }
}
