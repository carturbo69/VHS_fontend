namespace VHS_frontend.Areas.Admin.Models.Tag
{
    public class TagCreateDTO
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
    }
}
