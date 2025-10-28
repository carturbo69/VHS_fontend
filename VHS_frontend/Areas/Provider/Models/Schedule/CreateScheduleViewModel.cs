namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class CreateScheduleViewModel
    {
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int? BookingLimit { get; set; }
    }
}




