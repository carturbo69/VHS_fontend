namespace VHS_frontend.Models
{
    public class ServiceItem
    {
        public int Id { get; set; }
        public Guid ServiceId { get; set; }  // Guid của service để link đến Details
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Image { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string WorkWindow { get; set; } = "";
        public decimal Price { get; set; }
        public decimal PriceFrom { get; set; }
        public string Unit { get; set; } = "/giờ";
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public int SalesCount { get; set; }
        public string Category { get; set; } = "";
        public int CategoryId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();  // Tags riêng của dịch vụ
        public DateTime? CreatedAt { get; set; }  // Thời gian tạo dịch vụ
    }
}
