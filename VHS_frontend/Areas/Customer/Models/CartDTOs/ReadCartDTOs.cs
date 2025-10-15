using VHS_frontend.Areas.Customer.Models.CartItemDTOs;

namespace VHS_frontend.Areas.Customer.Models.CartDTOs
{
    public class ReadCartDTOs
    {
        public Guid CartId { get; set; }
        public Guid UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ReadCartItemDTOs> CartItems { get; set; } = new();
    }
}
