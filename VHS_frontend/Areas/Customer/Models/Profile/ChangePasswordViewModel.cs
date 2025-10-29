using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.Profile
{
    /// <summary>
    /// ViewModel cho đổi mật khẩu
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Mật khẩu phải bao gồm chữ hoa, chữ thường và số")]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP không được để trống")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải có đúng 6 ký tự")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải là số")]
        [Display(Name = "Mã OTP")]
        public string OTP { get; set; } = string.Empty;
    }
}







