namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class ReadDailyLimitViewModel
    {
        public DateOnly Date { get; set; }
        public int OrderLimit { get; set; }
        public bool IsOffDay => OrderLimit == 0;
        public string? Reason { get; set; }
        public DateTime? CreatedAt { get; set; }
        
        /// <summary>
        /// Gets the day name in Vietnamese
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


