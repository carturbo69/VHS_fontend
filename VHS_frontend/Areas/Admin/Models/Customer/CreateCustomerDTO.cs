namespace VHS_frontend.Areas.Admin.Models.Customer
{
    public class CreateCustomerDTO
    {
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = "User";   // cố định
    }
}
