using Microsoft.AspNetCore.Http;

namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffUpdateViewModel
    {
        public Guid StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? CitizenID { get; set; }

        public List<IFormFile>? FaceImages { get; set; }
        public List<IFormFile>? CitizenIDImages { get; set; }

        public string? FaceImage { get; set; } // để hiển thị ảnh cũ
        public string? CitizenIDImage { get; set; }
    }
}
