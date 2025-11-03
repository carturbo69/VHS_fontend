namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class ApproveProviderPaymentRequestDTO
    {
        public Guid BookingId { get; set; }
        public string AdminNote { get; set; } = string.Empty;
    }
}


