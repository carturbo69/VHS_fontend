namespace VHS_frontend.Areas.Admin.Models.Customer
{
    public class CustomerDTO
    {
        public Guid Id { get; set; }                 // map từ AccountId
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } //= "User";   // luôn là User ở trang này
        public bool IsDeleted { get; set; }           // map từ IsDeleted của backend
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

