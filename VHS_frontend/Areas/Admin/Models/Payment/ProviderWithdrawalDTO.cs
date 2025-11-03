namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class ProviderWithdrawalDTO
    {
        public Guid WithdrawalId { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? AdminNote { get; set; }
        public Guid? ProcessedByAdminId { get; set; }
    }
}


