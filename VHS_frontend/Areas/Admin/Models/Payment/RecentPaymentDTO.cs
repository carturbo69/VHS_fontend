namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class RecentPaymentDTO
    {
        public Guid PaymentId { get; set; }
        public Guid BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}

