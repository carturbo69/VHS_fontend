using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;

namespace VHS_frontend.Models.ServiceDTOs
{
    public class ReadAllOptionOfServicesDTOs
    {
        // Thông tin service
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }

        public string Title { get; set; } = null!;

        public decimal Price { get; set; }

        public DateTime? CreatedAt { get; set; }
        public string? Status { get; set; }


        // Options trực tiếp của service
        public List<ReadServiceOptionDTOs> ServiceOptions { get; set; } = new();
    }
}
