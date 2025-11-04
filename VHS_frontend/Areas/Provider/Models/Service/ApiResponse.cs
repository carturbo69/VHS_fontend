namespace VHS_frontend.Areas.Provider.Models.Service
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public object? Errors { get; set; }
    }
}

