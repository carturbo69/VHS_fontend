namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class PlaceOrderItemOptionDto
    {
        public Guid OptionId { get; set; }
        public string? Value { get; set; } // Stores the value for the option (text input, selected value, etc.)
    }

    public class PlaceOrderItemDto
    {
        public Guid ServiceId { get; set; }
        public DateTime BookingTime { get; set; }
        public decimal UnitPrice { get; set; }
        public List<PlaceOrderItemOptionDto> Options { get; set; } = new();
    }

    public class PlaceOrderRequestDto
    {
        public Guid UserId { get; set; }
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "";   // "VNPAY" | "MOMO"
        public string? VoucherCode { get; set; }
        public List<PlaceOrderItemDto> Items { get; set; } = new();
    }

    public class PlaceOrderResponseDto
    {
        public string ExternalTxnGroupId { get; set; } = "";
        public List<Guid> BookingIds { get; set; } = new();
        public List<Guid> PaymentIds { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class CreateGatewayRequestDto
    {
        public string GroupId { get; set; } = "";
        public decimal Amount { get; set; }
        public string ReturnUrl { get; set; } = "";
        public string IpnUrl { get; set; } = ""; // với MoMo
    }
}
