namespace VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs
{
    public class ReadServiceOptionDTOs
    {
        public Guid OptionId { get; set; }
        public string OptionName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = null!;
    }
}
