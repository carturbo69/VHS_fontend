namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class ScheduleOverviewViewModel
    {
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = "";
        public List<ReadScheduleViewModel> Schedules { get; set; } = new();
        public List<ReadTimeOffViewModel> UpcomingTimeOffs { get; set; } = new();
        public List<ReadDailyLimitViewModel> DailyLimits { get; set; } = new();
        public double TotalWeeklyHours => Schedules.Sum(s => s.WorkDuration);
        public bool IsAvailableToday { get; set; }
    }
}



