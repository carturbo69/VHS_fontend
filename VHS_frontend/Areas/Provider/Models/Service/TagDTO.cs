namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class TagDTO
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
    }
}

