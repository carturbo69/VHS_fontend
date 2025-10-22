namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class CancelBookingRequestDTO
    {
        public Guid BookingId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty; // <-- THÊM
        public string BankAccountNumber { get; set; } = string.Empty;
    }
}
