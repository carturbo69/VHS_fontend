namespace VHS_frontend.Areas.Provider.Models.Withdrawal
{
    public class ProviderWithdrawalDTO
    {
        public Guid WithdrawalId { get; set; }
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? QrCode { get; set; }
        public string? Note { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? AdminNote { get; set; }
    }
}


