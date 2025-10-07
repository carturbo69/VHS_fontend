namespace VHS_frontend.Models.Account
{
    public class ForgotPasswordDTO
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email không được để trống")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }
}
