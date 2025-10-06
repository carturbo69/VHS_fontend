namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffCreateViewModel
    {
        public string StaffName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? CitizenID { get; set; }
        public string? FaceImage { get; set; }
        public string? CitizenIDImage { get; set; }
    }
}
