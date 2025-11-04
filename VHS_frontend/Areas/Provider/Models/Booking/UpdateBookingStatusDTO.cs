namespace VHS_frontend.Areas.Provider.Models.Booking
{
    public class UpdateBookingStatusDTO
    {
        public Guid BookingId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class AssignStaffDTO
    {
        public Guid BookingId { get; set; }
        public Guid StaffId { get; set; }
    }
}

