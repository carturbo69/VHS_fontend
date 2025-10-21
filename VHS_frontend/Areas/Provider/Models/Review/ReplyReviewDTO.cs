using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Review
{
    public class ReplyReviewDTO
    {
        [Required(ErrorMessage = "Nội dung phản hồi không được để trống")]
        [MaxLength(1000, ErrorMessage = "Nội dung phản hồi không được vượt quá 1000 ký tự")]
        public string Reply { get; set; } = string.Empty;
    }
}

