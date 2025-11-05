using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VHS_frontend.Models.Account
{
    public class UpdateAccountDTO
    {
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Role { get; set; }
        public bool? IsDeleted { get; set; } // nếu cho admin xoá mềm
    }
}