namespace VHS_frontend.Areas.Admin.Models.Provider
{
    public class ProviderDTO
    {
        public Guid Id { get; set; }
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Password { get; set; }         // UI chỉ hiển thị •••••••
        public string Role { get; set; } = null!;
        public bool IsDeleted { get; set; }           // map từ IsDeleted của backend
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Tên nhà cung cấp (ProviderName) - không map từ backend, được lấy từ ProviderProfile
        public string ProviderName { get; set; } = string.Empty;
        // Số lượng dịch vụ chờ duyệt (Pending hoặc PendingUpdate) - không map từ backend, được tính ở frontend
        public int PendingUpdateCount { get; set; } = 0;
    }
}
