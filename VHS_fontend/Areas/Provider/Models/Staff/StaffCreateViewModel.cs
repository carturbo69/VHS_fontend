using Microsoft.AspNetCore.Http;

namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffCreateViewModel
    {
        public string StaffName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? CitizenID { get; set; }

        // 🖼 Cho phép upload nhiều ảnh
        public List<IFormFile>? FaceImages { get; set; }
        public List<IFormFile>? CitizenIDImages { get; set; }
    }
}
