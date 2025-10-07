using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VHS_frontend.Models.Account
{
    public class ReadAccountDTO
    {
        public Guid Id { get; set; }
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}