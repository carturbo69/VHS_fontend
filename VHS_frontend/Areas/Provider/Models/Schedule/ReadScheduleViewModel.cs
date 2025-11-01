using System.Text.Json.Serialization;

namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class ReadScheduleViewModel
    {
        public Guid ScheduleId { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName => GetDayName(DayOfWeek);
        
        [JsonConverter(typeof(TimeOnlyStringConverter))]
        public TimeOnly StartTime { get; set; }
        
        [JsonConverter(typeof(TimeOnlyStringConverter))]
        public TimeOnly EndTime { get; set; }
        public int? BookingLimit { get; set; }
        public DateTime? CreatedAt { get; set; }
        public double WorkDuration => (EndTime - StartTime).TotalHours;

        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Chủ nhật",
                1 => "Thứ 2",
                2 => "Thứ 3",
                3 => "Thứ 4",
                4 => "Thứ 5",
                5 => "Thứ 6",
                6 => "Thứ 7",
                _ => "Unknown"
            };
        }
    }
}



