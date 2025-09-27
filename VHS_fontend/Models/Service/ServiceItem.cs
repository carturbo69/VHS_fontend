namespace VHS_frontend.Models
{
    public class ServiceItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string WorkWindow { get; set; } = "";
        public decimal PriceFrom { get; set; }
        public string Unit { get; set; } = "/giờ";
        public double Rating { get; set; }
        public int RatingCount { get; set; }
    }
}
