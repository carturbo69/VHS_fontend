namespace VHS_frontend.Areas.Admin.Models.Customer
{
    public class UpdateCustomerDTO
    {
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = "User";   // giữ nguyên User
        public string? Password { get; set; }        // null = không đổi
    }
}
