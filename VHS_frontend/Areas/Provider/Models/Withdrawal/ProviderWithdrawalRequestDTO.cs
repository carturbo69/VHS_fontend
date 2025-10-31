using System.ComponentModel.DataAnnotations;

namespace VHS_frontend.Areas.Provider.Models.Withdrawal
{
    public class ProviderWithdrawalRequestDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
        [StringLength(50, ErrorMessage = "Số tài khoản không được quá 50 ký tự")]
        public string BankAccount { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng")]
        [StringLength(100, ErrorMessage = "Tên ngân hàng không được quá 100 ký tự")]
        public string BankName { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Mã QR không được quá 500 ký tự")]
        public string? QrCode { get; set; }
        
        [StringLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự")]
        public string? Note { get; set; }
    }
}


