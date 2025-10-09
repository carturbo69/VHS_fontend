using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace VHS_frontend.Models.ServiceDTOs
{
    public class ListServiceHomePageDTOs
    {
        public Guid ServiceId { get; set; }
        public Guid ProviderId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string UnitType { get; set; } = null!;
        public string? Images { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Status { get; set; }
        public bool? Deleted { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
