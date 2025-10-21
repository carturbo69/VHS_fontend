using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffCreateDTO
    {
        [Required(ErrorMessage = "Tên nhân viên không được để trống")]
        [StringLength(100, ErrorMessage = "Tên nhân viên không được vượt quá 100 ký tự")]
        public string StaffName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        public string Password { get; set; } = string.Empty;

        // Face image - 1 frontal photo only
        [Required(ErrorMessage = "Ảnh chân dung chính diện là bắt buộc")]
        public IFormFile? FaceImage { get; set; }

        [Required(ErrorMessage = "CCCD/CMND là bắt buộc")]
        [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD/CMND phải từ 9-12 ký tự")]
        [RegularExpression(@"^\d{9,12}$", ErrorMessage = "CCCD/CMND phải là 9-12 chữ số")]
        public string CitizenID { get; set; } = string.Empty;

        // CCCD front image
        [Required(ErrorMessage = "Phải tải lên ảnh mặt trước CCCD")]
        public IFormFile? CitizenIDFrontImage { get; set; }

        // CCCD back image
        [Required(ErrorMessage = "Phải tải lên ảnh mặt sau CCCD")]
        public IFormFile? CitizenIDBackImage { get; set; }
    }
}
