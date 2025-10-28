namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class CreateScheduleViewModel
    {
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int? BookingLimit { get; set; }
    }
}





