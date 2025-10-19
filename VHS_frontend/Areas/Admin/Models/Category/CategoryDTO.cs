namespace VHS_frontend.Areas.Admin.Models.Category
{
    public class CategoryDTO
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public bool? Deleted { get; set; }
    }
}
