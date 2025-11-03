using VHS_frontend.Models;

namespace VHS_frontend.Models.ServiceShop
{
    public class ServiceShopViewModel
    {
        public Guid ProviderId { get; set; }
        public ShopInfo ShopInfo { get; set; } = new ShopInfo();
        public List<ServiceItem> BestsellingServices { get; set; } = new List<ServiceItem>();
        public List<ServiceCategory> ShopCategories { get; set; } = new List<ServiceCategory>();
        public List<ServiceCategory> AllCategories { get; set; } = new List<ServiceCategory>();
        public List<ServiceItem> Services { get; set; } = new List<ServiceItem>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int? SelectedCategoryId { get; set; }
        public Guid? SelectedTagId { get; set; }
        public string SortBy { get; set; } = "popular";
    }

    public class ShopInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline";
        public string LastOnline { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int Following { get; set; }
        public int Followers { get; set; }
        public double ResponseRate { get; set; }
        public double Rating { get; set; }
        public int TotalRatings { get; set; }
        public string JoinDate { get; set; } = string.Empty;
        public bool IsFollowed { get; set; }
    }

    public class ServiceCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int ServiceCount { get; set; }
        public List<ServiceCategory> SubCategories { get; set; } = new List<ServiceCategory>();
        public List<CategoryTag> Tags { get; set; } = new List<CategoryTag>();
    }

    public class CategoryTag
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ServiceCount { get; set; }
    }
}
