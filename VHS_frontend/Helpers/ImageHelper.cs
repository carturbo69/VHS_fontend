using Microsoft.Extensions.Configuration;

namespace VHS_frontend.Helpers
{
    public static class ImageHelper
    {
      
        public static string GetImageUrl(string? imagePath, IConfiguration? configuration = null)
        {
            // Nếu path rỗng hoặc null, trả về empty string
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return string.Empty;
            }
            // Nếu đã là URL đầy đủ (bắt đầu bằng http:// hoặc https://), trả về luôn
            if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return imagePath;
            }

            // Lấy base URL từ configuration
            string baseUrl = "https://apivhs.cuahangkinhdoanh.com"; // Fallback mặc định
            
            if (configuration != null)
            {
                baseUrl = configuration["Apis:Backend"] ?? baseUrl;
            }

            // Kết hợp base URL với path (đảm bảo không có double slash)
            string normalizedPath = imagePath.StartsWith("/") ? imagePath : "/" + imagePath;
            return $"{baseUrl.TrimEnd('/')}{normalizedPath}";
        }
        public static string GetImageUrl(string? imagePath, string baseUrl = "https://apivhs.cuahangkinhdoanh.com")
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return string.Empty;
            }

            if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu đã là URL đầy đủ, trả về luôn
                return imagePath;
            }

            string normalizedPath = imagePath.StartsWith("/") ? imagePath : "/" + imagePath;
            return $"{baseUrl.TrimEnd('/')}{normalizedPath}";
        }
    }
}


