namespace VHS_frontend.Areas.Provider.Models
{
    public class ServiceViewModel
    {
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = "Hour";
        public int BaseUnit { get; set; }
        public string? Images { get; set; }
        public string Status { get; set; } = "Active";
    }
}
