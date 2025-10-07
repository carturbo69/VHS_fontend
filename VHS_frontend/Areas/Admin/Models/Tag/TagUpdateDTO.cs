namespace VHS_frontend.Areas.Admin.Models.Tag
{
    public class TagUpdateDTO
    {
        public string Name { get; set; } = null!;
        // Nếu cho phép chuyển danh mục, thêm: public Guid CategoryId { get; set; }
    }
}
