namespace VHS_frontend.Models.ServiceShop
{
    /// <summary>
    /// Wrapper cho API response
    /// </summary>
    public class ApiResponseWrapper<T>
    {
        public bool success { get; set; }
        public T? data { get; set; }
    }

    /// <summary>
    /// DTO cho Service từ API ServiceProvider/provider/{providerId}
    /// </summary>
    public class ServiceProviderReadDTO
    {
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string UnitType { get; set; } = string.Empty;
        public int BaseUnit { get; set; }
        public string? Images { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<TagDTO>? Tags { get; set; }
    }

    /// <summary>
    /// DTO cho Tag từ API
    /// </summary>
    public class TagDTO
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public bool? IsDeleted { get; set; }
    }
}

