using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.Profile
{
    /// <summary>
    /// ViewModel cho trang hiển thị profile
    /// </summary>
    public class ProfileViewModel
    {
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Images { get; set; }
        public string Address { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsProfileComplete { get; set; }

        /// <summary>
        /// Lấy URL ảnh profile hoặc ảnh mặc định
        /// </summary>
        public string GetProfileImageUrl()
        {
            if (!string.IsNullOrEmpty(Images))
            {
                return Images.StartsWith("http") ? Images : $"/{Images}";
            }
            return "/images/default-avatar.png";
        }

        /// <summary>
        /// Kiểm tra xem có ảnh profile không
        /// </summary>
        public bool HasProfileImage => !string.IsNullOrEmpty(Images);

        /// <summary>
        /// Lấy tên hiển thị (FullName hoặc AccountName)
        /// </summary>
        public string GetDisplayName()
        {
            return !string.IsNullOrEmpty(FullName) ? FullName : AccountName;
        }

        /// <summary>
        /// Tính phần trăm hoàn thiện profile
        /// </summary>
        public int GetProfileCompletionPercentage()
        {
            int completed = 0;
            int total = 6; // Tổng số trường bắt buộc

            if (!string.IsNullOrEmpty(AccountName)) completed++;
            if (!string.IsNullOrEmpty(Email)) completed++;
            if (!string.IsNullOrEmpty(FullName)) completed++;
            if (!string.IsNullOrEmpty(PhoneNumber)) completed++;
            if (!string.IsNullOrEmpty(Address)) completed++;
            if (!string.IsNullOrEmpty(Images)) completed++;

            return (int)Math.Round((double)completed / total * 100);
        }
    }

    /// <summary>
    /// ViewModel cho form chỉnh sửa profile
    /// </summary>
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Tên tài khoản là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên tài khoản không được vượt quá 100 ký tự")]
        [Display(Name = "Tên tài khoản")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Ảnh hiện tại (để hiển thị)
        /// </summary>
        public string? CurrentImage { get; set; }

        /// <summary>
        /// Lấy URL ảnh profile hiện tại
        /// </summary>
        public string GetCurrentImageUrl()
        {
            if (!string.IsNullOrEmpty(CurrentImage))
            {
                return CurrentImage.StartsWith("http") ? CurrentImage : $"/{CurrentImage}";
            }
            return "/images/default-avatar.png";
        }
    }

    /// <summary>
    /// ViewModel cho form đổi mật khẩu
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Mật khẩu phải bao gồm chữ hoa, chữ thường và số")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có đúng 6 số")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP chỉ được chứa số")]
        [Display(Name = "Mã OTP")]
        public string OTP { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái đã gửi OTP chưa
        /// </summary>
        public bool OTPSent { get; set; } = false;

        /// <summary>
        /// Thời gian còn lại để gửi lại OTP (giây)
        /// </summary>
        public int OTPCooldownSeconds { get; set; } = 0;
    }

    /// <summary>
    /// ViewModel cho form đổi email
    /// </summary>
    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "Email mới là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có đúng 6 số")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP chỉ được chứa số")]
        [Display(Name = "Mã OTP")]
        public string OTP { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái đã gửi OTP chưa
        /// </summary>
        public bool OTPSent { get; set; } = false;

        /// <summary>
        /// Thời gian còn lại để gửi lại OTP (giây)
        /// </summary>
        public int OTPCooldownSeconds { get; set; } = 0;
    }

    /// <summary>
    /// DTO để gửi lên API Backend
    /// </summary>
    public class EditProfileDTO
    {
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Images { get; set; }
        public string? Address { get; set; }
    }

    /// <summary>
    /// DTO để gửi lên API Backend cho đổi mật khẩu
    /// </summary>
    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO để gửi lên API Backend cho đổi email
    /// </summary>
    public class ChangeEmailDTO
    {
        public string NewEmail { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response từ API Backend
    /// </summary>
    public class ProfileResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// DTO nhận từ API Backend cho profile
    /// </summary>
    public class ViewProfileDTO
    {
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Images { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsProfileComplete { get; set; }
    }
}
