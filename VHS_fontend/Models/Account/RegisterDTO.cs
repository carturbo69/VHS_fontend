using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VHS_fontend.Models.Account
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Vui lòng nh?p tên ??ng nh?p")]
        [MinLength(3, ErrorMessage = "Tên ??ng nh?p t?i thi?u 3 ký t?")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nh?p email")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nh?p m?t kh?u")]
        [MinLength(6, ErrorMessage = "M?t kh?u t?i thi?u 6 ký t?")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nh?n m?t kh?u")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "M?t kh?u xác nh?n không kh?p")]
        public string ConfirmPassword { get; set; }
    }
}