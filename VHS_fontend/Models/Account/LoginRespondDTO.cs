using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VHS_fontend.Models.Account
{
    public class LoginRespondDTO
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public Guid AccountID { get; set; }
    }
}