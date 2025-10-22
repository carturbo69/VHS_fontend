namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class CategoryDTO
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

