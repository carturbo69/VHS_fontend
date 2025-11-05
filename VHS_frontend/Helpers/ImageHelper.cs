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
            string baseUrl = "https://localhost:5154"; // Fallback mặc định - dùng HTTPS
            
            if (configuration != null)
            {
                baseUrl = configuration["Apis:Backend"] ?? baseUrl;
            }

            // Nếu baseUrl là HTTP, chuyển sang HTTPS để tránh mixed content
            if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = baseUrl.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
            }

            // Kết hợp base URL với path (đảm bảo không có double slash)
            string normalizedPath = imagePath.StartsWith("/") ? imagePath : "/" + imagePath;
            return $"{baseUrl.TrimEnd('/')}{normalizedPath}";
        }
        public static string GetImageUrl(string? imagePath, string baseUrl = "https://localhost:5154")
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return string.Empty;
            }

            if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu là HTTP URL, chuyển sang HTTPS để tránh mixed content
                if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    return imagePath.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
                }
                return imagePath;
            }

            // Nếu baseUrl là HTTP, chuyển sang HTTPS
            if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = baseUrl.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
            }

            string normalizedPath = imagePath.StartsWith("/") ? imagePath : "/" + imagePath;
            return $"{baseUrl.TrimEnd('/')}{normalizedPath}";
        }
    }
}


