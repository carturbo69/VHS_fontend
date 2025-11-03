namespace VHS_frontend.Areas.Provider.Models.Withdrawal
{
    public class ProviderBalanceDTO
    {
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public decimal TotalEarnings { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal PendingWithdrawals { get; set; }
        public decimal AvailableBalance { get; set; }
        public int CompletedBookingsCount { get; set; }
        public int PendingWithdrawalCount { get; set; }
        public decimal GrossCompletedAmount { get; set; }
    }
}


