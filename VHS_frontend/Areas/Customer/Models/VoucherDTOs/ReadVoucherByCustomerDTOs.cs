namespace VHS_frontend.Areas.Customer.Models.VoucherDTOs
{
    public class ReadVoucherByCustomerDTOs
    {
        public Guid VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountMaxAmount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; }

        // Giới hạn & lượt dùng
        public int? UsageLimit { get; set; }     // null = không giới hạn
        public int UsedCount { get; set; }       // đã dùng
        public int? RemainingUses { get; set; }  // null = không giới hạn
        public bool IsUnlimited { get; set; }    // true nếu UsageLimit == null

        // Trạng thái tính theo thời gian hiện tại
        public bool IsExpired { get; set; }      // đã hết hạn
        public bool IsUpcoming { get; set; }     // chưa bắt đầu
        public bool CanUseNow { get; set; }      // dùng được ngay bây giờ
    }
}
