using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Profile
{
    public class ProviderProfileUpdateDTO
    {
        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(100, ErrorMessage = "Tên nhà cung cấp không được vượt quá 100 ký tự")]
        public string ProviderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;

        // Hidden field để lưu URL hình ảnh sau khi upload
        public string Images { get; set; } = string.Empty;

        // File upload property
        public IFormFile? ImageFile { get; set; }
    }
}
