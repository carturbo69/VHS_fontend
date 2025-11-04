using System;

namespace VHS_frontend.Areas.Provider.Models.Staff
{
    public class StaffScheduleDTO
    {
        public Guid BookingId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!;
        public string Address { get; set; } = null!;
    }

    public class StaffScheduleResponse
    {
        public Guid StaffId { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public List<StaffScheduleDTO> Schedule { get; set; } = new List<StaffScheduleDTO>();
    }
}

