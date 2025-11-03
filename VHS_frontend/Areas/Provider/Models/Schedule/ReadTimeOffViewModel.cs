using System.Text.Json.Serialization;

namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class ReadTimeOffViewModel
    {
        public Guid TimeOffId { get; set; }
        public DateOnly Date { get; set; }
        
        [JsonConverter(typeof(NullableTimeOnlyStringConverter))]
        public TimeOnly? StartTime { get; set; }
        
        [JsonConverter(typeof(NullableTimeOnlyStringConverter))]
        public TimeOnly? EndTime { get; set; }
        public string? Reason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsFullDay => !StartTime.HasValue && !EndTime.HasValue;
        public double? Duration => StartTime.HasValue && EndTime.HasValue ? (EndTime.Value - StartTime.Value).TotalHours : null;
        
        /// <summary>
        /// Gets the day name in Vietnamese (Chủ nhật, Thứ 2, Thứ 3, etc.)
        /// </summary>
        public string DayName
        {
            get
            {
                var dayOfWeek = Date.DayOfWeek;
                return dayOfWeek switch
                {
                    DayOfWeek.Sunday => "Chủ nhật",
                    DayOfWeek.Monday => "Thứ 2",
                    DayOfWeek.Tuesday => "Thứ 3",
                    DayOfWeek.Wednesday => "Thứ 4",
                    DayOfWeek.Thursday => "Thứ 5",
                    DayOfWeek.Friday => "Thứ 6",
                    DayOfWeek.Saturday => "Thứ 7",
                    _ => dayOfWeek.ToString()
                };
            }
        }
    }
}


