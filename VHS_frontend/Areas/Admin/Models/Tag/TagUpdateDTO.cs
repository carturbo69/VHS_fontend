namespace VHS_frontend.Areas.Admin.Models.Tag
{
    public class TagUpdateDTO
    {
        public string Name { get; set; } = null!;
        public Guid CategoryId { get; set; }  // Cho phép chuyển tag sang category khác
    }
}
