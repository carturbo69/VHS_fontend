    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;
    namespace VHS_frontend.Models.ServiceDTOs
    {
        public class ListServiceHomePageDTOs
        {
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
            public bool? Deleted { get; set; }
            public double AverageRating { get; set; }
            public int TotalReviews { get; set; }

            public string CategoryName { get; set; } = null!;
            
            public string? ProviderName { get; set; }
            
            // Thêm Options để hiển thị trong card
            public List<ReadServiceOptionDTOs> ServiceOptions { get; set; } = new();
    }
    }
