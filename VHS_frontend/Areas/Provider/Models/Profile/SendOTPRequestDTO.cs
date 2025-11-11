using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Profile
{
    public class SendOTPRequestDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;
    }
}

