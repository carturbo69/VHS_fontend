namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class CreateTimeOffViewModel
    {
        public DateOnly Date { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Reason { get; set; }
    }
}





