using System;
using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Admin.Models.Notification
{
    public class AdminNotificationCreateDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn người nhận")]
        public Guid AccountReceivedId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò người nhận")]
        public string ReceiverRole { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn loại thông báo")]
        public string NotificationType { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập nội dung thông báo")]
        [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string Content { get; set; } = null!;
    }
}
