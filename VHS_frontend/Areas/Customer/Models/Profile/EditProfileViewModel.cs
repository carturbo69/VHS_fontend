using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.Profile
{
    /// <summary>
    /// ViewModel cho chỉnh sửa profile
    /// </summary>
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MinLength(8, ErrorMessage = "Tối thiểu 8 ký tự, chữ và số, không dấu, không khoảng cách")]
        [StringLength(100, ErrorMessage = "Tên đăng nhập không được quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9]{8,}$", ErrorMessage = "Tối thiểu 8 ký tự, chữ và số, không dấu, không khoảng cách")]
        [Display(Name = "Tên đăng nhập")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Họ và tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(11, ErrorMessage = "Số điện thoại không được quá 11 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        /// <summary>
        /// URL ảnh hiện tại của profile
        /// </summary>
        [Display(Name = "Ảnh hiện tại")]
        public string? CurrentImage { get; set; }

        /// <summary>
        /// Lấy URL của ảnh hiện tại, nếu không có thì trả về ảnh mặc định
        /// </summary>
        public string GetCurrentImageUrl()
        {
            return !string.IsNullOrEmpty(CurrentImage) ? CurrentImage : "/images/default-avatar.png";
        }
    }
}


