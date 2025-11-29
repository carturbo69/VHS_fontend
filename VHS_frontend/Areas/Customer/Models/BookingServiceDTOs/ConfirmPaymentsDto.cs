namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class ConfirmPaymentsDto
    {
        public List<Guid> BookingIds { get; set; } = new();
        public string PaymentMethod { get; set; } = null!; // "VNPAY", "MOMO", ...
        public string? GatewayTxnId { get; set; }          // mã giao dịch từ cổng

        // mới: truyền thẳng các CartItemId từ session (nếu có)
        public List<Guid>? CartItemIdsForCleanup { get; set; }
        
        // Thời gian thanh toán (theo timezone Việt Nam UTC+7)
        public DateTime? PaymentTime { get; set; }
    }
}
