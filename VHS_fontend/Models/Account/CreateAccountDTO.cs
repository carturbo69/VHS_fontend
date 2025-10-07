using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VHS_frontend.Models.Account
{
    public class CreateAccountDTO
    {
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Role { get; set; } // Optional nếu không do admin tạo
    }
}