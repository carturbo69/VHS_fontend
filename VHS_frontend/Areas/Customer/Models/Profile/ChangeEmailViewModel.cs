using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.Profile
{
    /// <summary>
    /// ViewModel cho đổi email
    /// </summary>
    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "Email mới không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP không được để trống")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải có đúng 6 ký tự")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải là số")]
        [Display(Name = "Mã OTP")]
        public string OTP { get; set; } = string.Empty;
    }
}




