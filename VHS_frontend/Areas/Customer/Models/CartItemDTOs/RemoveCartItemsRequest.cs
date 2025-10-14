    namespace VHS_frontend.Areas.Customer.Models.CartItemDTOs
    {
        public class RemoveCartItemsRequest
        {
            public List<Guid> CartItemIds { get; set; } = new();
        }
    }
