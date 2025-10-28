using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.Profile
{
    /// <summary>
    /// ViewModel cho trang profile
    /// </summary>
    public class ProfileViewModel
    {
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        
        [Display(Name = "Tên đăng nhập")]
        public string AccountName { get; set; } = string.Empty;
        
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = string.Empty;
        
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }
        
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }
        
        [Display(Name = "Ảnh đại diện")]
        public string? Images { get; set; }
        
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
        
        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; }
        
        [Display(Name = "Cập nhật lần cuối")]
        public DateTime? UpdatedAt { get; set; }
        
        [Display(Name = "Hoàn thiện profile")]
        public bool IsProfileComplete { get; set; }

        /// <summary>
        /// Lấy URL của ảnh đại diện, nếu không có thì trả về ảnh mặc định
        /// </summary>
        public string GetProfileImageUrl()
        {
            return !string.IsNullOrEmpty(Images) ? Images : "/images/default-avatar.png";
        }

        /// <summary>
        /// Kiểm tra xem có ảnh đại diện hay không
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
            int completedFields = 0;
            int totalFields = 5; // AccountName, Email, FullName, PhoneNumber, Address, Images

            // AccountName và Email luôn có (không tính)
            if (!string.IsNullOrEmpty(FullName)) completedFields++;
            if (!string.IsNullOrEmpty(PhoneNumber)) completedFields++;
            if (!string.IsNullOrEmpty(Address)) completedFields++;
            if (!string.IsNullOrEmpty(Images)) completedFields++;

            return (int)Math.Round((double)completedFields / totalFields * 100);
        }
    }
}


