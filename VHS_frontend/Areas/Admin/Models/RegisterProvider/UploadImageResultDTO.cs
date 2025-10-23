namespace VHS_frontend.Areas.Admin.Models.RegisterProvider
{
    // Kết quả API upload avatar
    public class UploadImageResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ImagePath { get; set; } // "/profile-images/xxx.jpg" hoặc "/provider-avatar/xxx.jpg"
        public string? ImageUrl { get; set; }  // Nếu backend trả absolute
    }
}
