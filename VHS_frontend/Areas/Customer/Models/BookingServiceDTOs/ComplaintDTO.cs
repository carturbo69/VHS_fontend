using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    public class ComplaintDTO
    {
        //public Guid BookingId { get; set; }
        //public string ComplaintType { get; set; } = string.Empty;
        //public string? Description { get; set; }

        //// Thông tin hiển thị (product/service)
        //public string ServiceTitle { get; set; } = string.Empty;
        //public string ProviderName { get; set; } = string.Empty;
        //public string ServiceImage { get; set; } = string.Empty;
        //public decimal Price { get; set; }
        //public decimal OriginalPrice { get; set; }

        [Required]
        public Guid BookingId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lý do")]
        [StringLength(200)]
        public string ComplaintType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [StringLength(2000)]
        public string? Description { get; set; }

        // Dùng để hiển thị trên trang báo cáo
        public string ServiceTitle { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceImage { get; set; } = "/images/VeSinh.jpg";
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
    }
}
