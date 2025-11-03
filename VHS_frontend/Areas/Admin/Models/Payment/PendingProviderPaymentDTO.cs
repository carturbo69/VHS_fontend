namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class PendingProviderPaymentDTO
    {
        public Guid BookingId { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderEmail { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServiceAmount { get; set; }
        public decimal ProviderCommission { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime CompletionDate { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
}


