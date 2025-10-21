using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Admin.Models.Voucher
{
    public class AdminVoucherEditDTO
    {
        public Guid? VoucherId { get; set; }

        [Required, MaxLength(64)]
        public string Code { get; set; } = null!;

        [MaxLength(512)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountMaxAmount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Range(0, int.MaxValue)]
        public int? UsageLimit { get; set; }

        public bool? IsActive { get; set; } = true;
    }
}
