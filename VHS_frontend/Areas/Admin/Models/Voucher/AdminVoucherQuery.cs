namespace VHS_frontend.Areas.Admin.Models.Voucher
{
    public class AdminVoucherQuery
    {
        public string? Keyword { get; set; }
        public bool? OnlyActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
