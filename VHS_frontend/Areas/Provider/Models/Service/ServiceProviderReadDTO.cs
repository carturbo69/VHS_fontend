namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class ServiceProviderReadDTO
    {
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string UnitType { get; set; } = string.Empty;
        public int BaseUnit { get; set; }
        public string? Images { get; set; }
        public string? Status { get; set; }
        public List<TagDTO> Tags { get; set; } = new List<TagDTO>();
        public List<OptionDTO> Options { get; set; } = new List<OptionDTO>();
    }
}

