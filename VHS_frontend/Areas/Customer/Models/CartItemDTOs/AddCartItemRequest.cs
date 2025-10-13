namespace VHS_frontend.Areas.Customer.Models.CartItemDTOs
{
    public class AddCartItemRequest
    {
        public Guid ServiceId { get; set; }
        // Nếu service không có option thì có thể để rỗng hoặc []
        public List<Guid>? OptionIds { get; set; } = new();
    }
}
