using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VHS_frontend.Areas.Customer.Models.ReviewDTOs
{
    public class CreateReviewDTOs
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public Guid ServiceId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        // Nhiều ảnh upload kèm đánh giá
        public List<IFormFile>? ImageFiles { get; set; }
    }

}
