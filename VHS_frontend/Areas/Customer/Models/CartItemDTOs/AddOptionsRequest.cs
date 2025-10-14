using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.CartItemDTOs
{
    public class AddOptionsRequest
    {
        [Required]
        public List<Guid> OptionIds { get; set; } = new();
    }
}
