namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class UnconfirmedBookingRefundDTO
    {
        public Guid BookingId { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaymentCreatedAt { get; set; }

        // Bổ sung thêm các trường để view và nghiệp vụ không lỗi
        public string? CancelReason { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
    }
}


