using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Tag
{
    public class TagCreateDTO
    {
        [Required(ErrorMessage = "CategoryId không được để trống.")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Tên tag không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên tag không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }
    }
}