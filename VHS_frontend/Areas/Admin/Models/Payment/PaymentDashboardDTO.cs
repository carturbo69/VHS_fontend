namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class PaymentDashboardDTO
    {
        public int PendingRefunds { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public int PendingPayouts { get; set; }
        public decimal TotalPayoutAmount { get; set; }
        public decimal SystemEscrowBalance { get; set; }
        public PaymentStatisticsDTO? Statistics { get; set; }
        public List<RecentTransactionDTO>? RecentTransactions { get; set; }
    }

    public class PaymentStatisticsDTO
    {
        public decimal TotalRefundsThisMonth { get; set; }
        public decimal TotalPayoutsThisMonth { get; set; }
        public int RefundsProcessedToday { get; set; }
        public int PayoutsProcessedToday { get; set; }
        public decimal AverageRefundAmount { get; set; }
        public decimal AveragePayoutAmount { get; set; }
    }

    public class RecentTransactionDTO
    {
        public Guid TransactionId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}


