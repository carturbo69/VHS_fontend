namespace VHS_frontend.Areas.Admin.Models.Tag
{
    public class TagDTO
    {
        public Guid TagId { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public bool? IsDeleted { get; set; }
    }
}
