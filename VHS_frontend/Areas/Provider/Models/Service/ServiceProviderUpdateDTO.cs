using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class ServiceProviderUpdateDTO
    {
        [Required(ErrorMessage = "Tên dịch vụ không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên dịch vụ không được vượt quá 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Đơn vị không được để trống.")]
        public string UnitType { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Đơn vị cơ bản phải lớn hơn 0.")]
        public int BaseUnit { get; set; }

        public IFormFile? Avatar { get; set; }
        public List<IFormFile>? Images { get; set; }
        public List<string> RemoveImages { get; set; } = new List<string>();

        public string? Status { get; set; }

        public List<Guid> TagIds { get; set; } = new List<Guid>();
        public List<Guid> OptionIds { get; set; } = new List<Guid>();
    }
}

