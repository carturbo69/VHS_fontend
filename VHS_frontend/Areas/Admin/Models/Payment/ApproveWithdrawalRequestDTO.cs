namespace VHS_frontend.Areas.Admin.Models.Payment
{
    public class ApproveWithdrawalRequestDTO
    {
        public Guid WithdrawalId { get; set; }
        public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
        public string AdminNote { get; set; } = string.Empty;
    }
}


