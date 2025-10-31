namespace VHS_frontend.Areas.Customer.Models.Profile
{
    /// <summary>
    /// Response từ Backend API
    /// </summary>
    public class ProfileResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

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

    public class OTPResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
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
    /// DTO để gửi lên API Backend cho cập nhật profile
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
}




