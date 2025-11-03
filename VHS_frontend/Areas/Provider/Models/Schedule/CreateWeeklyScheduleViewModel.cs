namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class CreateWeeklyScheduleViewModel
    {
        public List<int> DaysOfWeek { get; set; } = new();
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int? BookingLimit { get; set; }
    }
}

