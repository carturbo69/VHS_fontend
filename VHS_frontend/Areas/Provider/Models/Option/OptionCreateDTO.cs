using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Option
{
    public class OptionCreateDTO
    {
        [Required(ErrorMessage = "Tên option không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên option không được vượt quá 100 ký tự.")]
        public string OptionName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Đơn vị không được để trống.")]
        public string UnitType { get; set; } = string.Empty;
    }
}