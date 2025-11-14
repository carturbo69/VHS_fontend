
using VHS_frontend.Areas.Customer.Models.ReviewDTOs;
using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;

namespace VHS_frontend.Models.ServiceDTOs
{
    public class ServiceDetailDTOs
    {
        // Thông tin service
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = null!;
        public int? BaseUnit { get; set; }
        public string? Images { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Status { get; set; }

        public string CategoryName { get; set; } = null!;

        // Tổng quan review của chính service này
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        // Danh sách review chi tiết
        public List<ReadReviewDTOs> Reviews { get; set; } = new();

        // Provider summary
        public ProviderSummaryDTOs Provider { get; set; } = new();

        // Options trực tiếp của service
        public List<ReadServiceOptionDTOs> ServiceOptions { get; set; } = new();

        // Tags của service
        public List<ServiceTagDTO> Tags { get; set; } = new();

        // Packages của service (mỗi package có danh sách option)
        //public List<ServicePackageDTO> Packages { get; set; } = new();
    }

    public class ServiceTagDTO
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
