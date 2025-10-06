namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffReadViewModel
    {
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string? CitizenID { get; set; }
        public string? FaceImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
