using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VHS_frontend.Models.Account
{
    public class LoginRespondDTO
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public Guid AccountID { get; set; }

        // Thêm:
        public string? DisplayName { get; set; }
    }
}