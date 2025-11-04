namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffDTO
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty; // CitizenID as username
        public string FaceImage { get; set; } = string.Empty;
        public string CitizenID { get; set; } = string.Empty;
        public string CitizenIDFrontImage { get; set; } = string.Empty;
        public string CitizenIDBackImage { get; set; } = string.Empty;
        public string Role { get; set; } = "Staff";
        public bool IsLocked { get; set; } = false; // Trạng thái khóa tài khoản
    }
}
