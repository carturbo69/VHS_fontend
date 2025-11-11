using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffUpdateDTO
    {
        [Required(ErrorMessage = "Tên nhân viên không được để trống")]
        [StringLength(100, ErrorMessage = "Tên nhân viên không được vượt quá 100 ký tự")]
        public string StaffName { get; set; } = string.Empty;

        // Face image - 1 frontal photo only (optional for update)
        public IFormFile? FaceImage { get; set; }

        [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD/CMND phải từ 9-12 ký tự")]
        [RegularExpression(@"^\d{9,12}$", ErrorMessage = "CCCD/CMND phải là 9-12 chữ số")]
        public string? CitizenID { get; set; }

        // CCCD front image (optional for update)
        public IFormFile? CitizenIDFrontImage { get; set; }

        // CCCD back image (optional for update)
        public IFormFile? CitizenIDBackImage { get; set; }

        // Current images (to keep existing ones)
        public string? CurrentFaceImage { get; set; }
        public string? CurrentCitizenIDFrontImage { get; set; }
        public string? CurrentCitizenIDBackImage { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string Address { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }
    }
}
