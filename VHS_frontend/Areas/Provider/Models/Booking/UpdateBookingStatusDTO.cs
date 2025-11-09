namespace VHS_frontend.Areas.Provider.Models.Booking
{
    public class UpdateBookingStatusDTO
    {
        public Guid BookingId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
        /// <summary>
        /// StaffId mà provider đã chọn trước khi xác nhận (tùy chọn)
        /// Nếu có, hệ thống sẽ kiểm tra và gán nhân viên này thay vì tự động gán
        /// </summary>
        public Guid? SelectedStaffId { get; set; }
    }

    public class AssignStaffDTO
    {
        public Guid BookingId { get; set; }
        public Guid StaffId { get; set; }
    }
}

