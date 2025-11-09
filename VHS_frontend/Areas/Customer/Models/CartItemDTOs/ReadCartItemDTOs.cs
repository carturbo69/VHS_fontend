namespace VHS_frontend.Areas.Customer.Models.CartItemDTOs
{
    public class ReadCartItemDTOs
    {
        public Guid CartItemId { get; set; }
        public Guid CartId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Thông tin dịch vụ
        public string? ServiceName { get; set; }
        public decimal? ServicePrice { get; set; }
        public string? ServiceImage { get; set; }

        // Provider của dịch vụ
        public Guid ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderImages { get; set; } // Logo/ảnh của provider

        // Tuỳ chọn của cart item (nếu dùng)
        public List<CartItemOptionReadDto> Options { get; set; } = new();
    }


    public class CartItemOptionReadDto
    {
        public Guid CartItemOptionId { get; set; }
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = null!;
        //public string? OptionValue { get; set; }
    }
}

