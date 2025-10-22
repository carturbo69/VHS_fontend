namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class OptionDTO
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = string.Empty;
    }
}

