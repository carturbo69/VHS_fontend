using VHS_frontend.Models;
using VHS_frontend.Models.ServiceShop;

namespace VHS_frontend.Services
{
    public class ServiceShopService
    {
        public ServiceShopService()
        {
        }
        public async Task<ServiceShopViewModel> GetServiceShopViewModelAsync(int? categoryId, string sortBy, int page)
        {
            // Mock data - trong thực tế sẽ gọi API hoặc database
            var viewModel = new ServiceShopViewModel
            {
                ShopInfo = new ShopInfo
                {
                    Id = 1,
                    Name = "Viet Home Service - VHS",
                    Logo = "/images/logo.png",
                    Status = "Online",
                    LastOnline = "3 phút trước",
                    TotalServices = 89,
                    Following = 156,
                    Followers = 2847,
                    ResponseRate = 99.2,
                    Rating = 4.9,
                    TotalRatings = 1247,
                    JoinDate = "3 năm trước",
                    IsFollowed = false
                },
                BestsellingServices = GetBestsellingServices(),
                ShopCategories = GetShopCategories(),
                AllCategories = GetAllCategories(),
                Services = GetServices(categoryId, sortBy, page),
                CurrentPage = page,
                TotalPages = 8,
                SelectedCategoryId = categoryId,
                SortBy = sortBy
            };

            return await Task.FromResult(viewModel);
        }

        public async Task<ServiceItem?> GetServiceByIdAsync(int id)
        {
            // Mock data
            var services = GetMockServices();
            return await Task.FromResult(services.FirstOrDefault(s => s.Id == id));
        }

        public async Task<List<ServiceItem>> GetBestsellingServicesAsync()
        {
            return await Task.FromResult(GetBestsellingServices());
        }

        public async Task<List<ServiceItem>> GetServicesByCategoryAsync(int categoryId, string sortBy, int page)
        {
            return await Task.FromResult(GetServices(categoryId, sortBy, page));
        }

        private List<ServiceItem> GetBestsellingServices()
        {
            return new List<ServiceItem>
            {
                new ServiceItem
                {
                    Id = 1,
                    Name = "Vệ sinh máy tính để bàn",
                    Description = "Dịch vụ vệ sinh máy tính để bàn chuyên nghiệp, làm sạch bụi bẩn, thay keo tản nhiệt",
                    Price = 150000,
                    Image = "/images/services/desktop-cleaning.jpg",
                    Rating = 4.9,
                    SalesCount = 156,
                    Category = "Vệ sinh máy tính"
                },
                new ServiceItem
                {
                    Id = 2,
                    Name = "Vệ sinh laptop",
                    Description = "Vệ sinh laptop chuyên sâu, thay keo tản nhiệt, làm sạch quạt tản nhiệt",
                    Price = 200000,
                    Image = "/images/services/laptop-cleaning.jpg",
                    Rating = 4.8,
                    SalesCount = 89,
                    Category = "Vệ sinh laptop"
                },
                new ServiceItem
                {
                    Id = 3,
                    Name = "Vệ sinh máy chủ server",
                    Description = "Dịch vụ vệ sinh máy chủ server chuyên nghiệp, bảo trì hệ thống",
                    Price = 500000,
                    Image = "/images/services/server-cleaning.jpg",
                    Rating = 4.7,
                    SalesCount = 45,
                    Category = "Vệ sinh máy chủ"
                },
                new ServiceItem
                {
                    Id = 4,
                    Name = "Vệ sinh thiết bị mạng",
                    Description = "Vệ sinh router, switch, modem và các thiết bị mạng khác",
                    Price = 100000,
                    Image = "/images/services/network-cleaning.jpg",
                    Rating = 4.6,
                    SalesCount = 78,
                    Category = "Vệ sinh thiết bị mạng"
                },
                new ServiceItem
                {
                    Id = 5,
                    Name = "Vệ sinh máy in",
                    Description = "Vệ sinh và bảo trì máy in, làm sạch đầu phun, thay mực",
                    Price = 120000,
                    Image = "/images/services/printer-cleaning.jpg",
                    Rating = 4.5,
                    SalesCount = 34,
                    Category = "Vệ sinh máy in"
                },
                new ServiceItem
                {
                    Id = 6,
                    Name = "Vệ sinh phòng máy",
                    Description = "Vệ sinh toàn bộ phòng máy tính, hệ thống làm mát",
                    Price = 800000,
                    Image = "/images/services/computer-room-cleaning.jpg",
                    Rating = 4.8,
                    SalesCount = 23,
                    Category = "Vệ sinh phòng máy"
                }
            };
        }

        private List<ServiceCategory> GetShopCategories()
        {
            return new List<ServiceCategory>
            {
                new ServiceCategory { Id = 1, Name = "Vệ sinh máy tính", Icon = "/images/categories/desktop.png", ServiceCount = 25 },
                new ServiceCategory { Id = 2, Name = "Vệ sinh laptop", Icon = "/images/categories/laptop.png", ServiceCount = 18 },
                new ServiceCategory { Id = 3, Name = "Vệ sinh máy chủ", Icon = "/images/categories/server.png", ServiceCount = 12 },
                new ServiceCategory { Id = 4, Name = "Vệ sinh thiết bị mạng", Icon = "/images/categories/network.png", ServiceCount = 8 },
                new ServiceCategory { Id = 5, Name = "Vệ sinh máy in", Icon = "/images/categories/printer.png", ServiceCount = 6 },
                new ServiceCategory { Id = 6, Name = "Vệ sinh phòng máy", Icon = "/images/categories/room.png", ServiceCount = 15 },
                new ServiceCategory { Id = 7, Name = "Bảo trì hệ thống", Icon = "/images/categories/maintenance.png", ServiceCount = 5 }
            };
        }

        private List<ServiceCategory> GetAllCategories()
        {
            return new List<ServiceCategory>
            {
                new ServiceCategory 
                { 
                    Id = 1, 
                    Name = "Vệ sinh máy tính", 
                    Icon = "/images/categories/desktop.png",
                    ServiceCount = 25,
                    SubCategories = new List<ServiceCategory>
                    {
                        new ServiceCategory { Id = 11, Name = "Vệ sinh CPU", ServiceCount = 8 },
                        new ServiceCategory { Id = 12, Name = "Vệ sinh RAM", ServiceCount = 6 },
                        new ServiceCategory { Id = 13, Name = "Vệ sinh ổ cứng", ServiceCount = 11 }
                    }
                },
                new ServiceCategory 
                { 
                    Id = 2, 
                    Name = "Vệ sinh laptop", 
                    Icon = "/images/categories/laptop.png",
                    ServiceCount = 18,
                    SubCategories = new List<ServiceCategory>
                    {
                        new ServiceCategory { Id = 21, Name = "Vệ sinh bàn phím", ServiceCount = 5 },
                        new ServiceCategory { Id = 22, Name = "Vệ sinh màn hình", ServiceCount = 7 },
                        new ServiceCategory { Id = 23, Name = "Vệ sinh quạt tản nhiệt", ServiceCount = 6 }
                    }
                },
                new ServiceCategory 
                { 
                    Id = 3, 
                    Name = "Vệ sinh máy chủ", 
                    Icon = "/images/categories/server.png",
                    ServiceCount = 12
                },
                new ServiceCategory 
                { 
                    Id = 4, 
                    Name = "Vệ sinh thiết bị mạng", 
                    Icon = "/images/categories/network.png",
                    ServiceCount = 8
                },
                new ServiceCategory 
                { 
                    Id = 5, 
                    Name = "Vệ sinh máy in", 
                    Icon = "/images/categories/printer.png",
                    ServiceCount = 6
                },
                new ServiceCategory 
                { 
                    Id = 6, 
                    Name = "Vệ sinh phòng máy", 
                    Icon = "/images/categories/room.png",
                    ServiceCount = 15
                }
            };
        }

        private List<ServiceItem> GetServices(int? categoryId, string sortBy, int page)
        {
            var allServices = GetMockServices();
            
            if (categoryId.HasValue)
            {
                allServices = allServices.Where(s => s.CategoryId == categoryId.Value).ToList();
            }

            // Sort logic
            switch (sortBy.ToLower())
            {
                case "newest":
                    allServices = allServices.OrderByDescending(s => s.Id).ToList();
                    break;
                case "bestselling":
                    allServices = allServices.OrderByDescending(s => s.SalesCount).ToList();
                    break;
                case "price":
                    allServices = allServices.OrderBy(s => s.Price).ToList();
                    break;
                default: // popular
                    allServices = allServices.OrderByDescending(s => s.Rating).ToList();
                    break;
            }

            // Pagination
            var pageSize = 20;
            var skip = (page - 1) * pageSize;
            return allServices.Skip(skip).Take(pageSize).ToList();
        }

        private List<ServiceItem> GetMockServices()
        {
            return new List<ServiceItem>
            {
                new ServiceItem { Id = 1, Name = "Vệ sinh máy tính để bàn", Description = "Dịch vụ vệ sinh máy tính để bàn chuyên nghiệp, làm sạch bụi bẩn, thay keo tản nhiệt", Price = 150000, Image = "/images/services/desktop-cleaning.jpg", Rating = 4.9, SalesCount = 156, Category = "Vệ sinh máy tính", CategoryId = 1 },
                new ServiceItem { Id = 2, Name = "Vệ sinh laptop", Description = "Vệ sinh laptop chuyên sâu, thay keo tản nhiệt, làm sạch quạt tản nhiệt", Price = 200000, Image = "/images/services/laptop-cleaning.jpg", Rating = 4.8, SalesCount = 89, Category = "Vệ sinh laptop", CategoryId = 2 },
                new ServiceItem { Id = 3, Name = "Vệ sinh máy chủ server", Description = "Dịch vụ vệ sinh máy chủ server chuyên nghiệp, bảo trì hệ thống", Price = 500000, Image = "/images/services/server-cleaning.jpg", Rating = 4.7, SalesCount = 45, Category = "Vệ sinh máy chủ", CategoryId = 3 },
                new ServiceItem { Id = 4, Name = "Vệ sinh thiết bị mạng", Description = "Vệ sinh router, switch, modem và các thiết bị mạng khác", Price = 100000, Image = "/images/services/network-cleaning.jpg", Rating = 4.6, SalesCount = 78, Category = "Vệ sinh thiết bị mạng", CategoryId = 4 },
                new ServiceItem { Id = 5, Name = "Vệ sinh máy in", Description = "Vệ sinh và bảo trì máy in, làm sạch đầu phun, thay mực", Price = 120000, Image = "/images/services/printer-cleaning.jpg", Rating = 4.5, SalesCount = 34, Category = "Vệ sinh máy in", CategoryId = 5 },
                new ServiceItem { Id = 6, Name = "Vệ sinh phòng máy", Description = "Vệ sinh toàn bộ phòng máy tính, hệ thống làm mát", Price = 800000, Image = "/images/services/computer-room-cleaning.jpg", Rating = 4.8, SalesCount = 23, Category = "Vệ sinh phòng máy", CategoryId = 6 },
                new ServiceItem { Id = 7, Name = "Vệ sinh CPU", Description = "Vệ sinh CPU và thay keo tản nhiệt chuyên nghiệp", Price = 80000, Image = "/images/services/cpu-cleaning.jpg", Rating = 4.7, SalesCount = 67, Category = "Vệ sinh máy tính", CategoryId = 1 },
                new ServiceItem { Id = 8, Name = "Vệ sinh RAM", Description = "Vệ sinh khe RAM và thanh RAM, làm sạch tiếp xúc", Price = 60000, Image = "/images/services/ram-cleaning.jpg", Rating = 4.6, SalesCount = 45, Category = "Vệ sinh máy tính", CategoryId = 1 },
                new ServiceItem { Id = 9, Name = "Vệ sinh ổ cứng", Description = "Vệ sinh ổ cứng HDD và SSD, làm sạch bụi bẩn", Price = 70000, Image = "/images/services/hdd-cleaning.jpg", Rating = 4.5, SalesCount = 38, Category = "Vệ sinh máy tính", CategoryId = 1 },
                new ServiceItem { Id = 10, Name = "Vệ sinh bàn phím laptop", Description = "Vệ sinh bàn phím laptop chuyên sâu, làm sạch bụi bẩn", Price = 90000, Image = "/images/services/keyboard-cleaning.jpg", Rating = 4.8, SalesCount = 56, Category = "Vệ sinh laptop", CategoryId = 2 },
                new ServiceItem { Id = 11, Name = "Vệ sinh màn hình", Description = "Vệ sinh màn hình máy tính, laptop chuyên nghiệp", Price = 50000, Image = "/images/services/screen-cleaning.jpg", Rating = 4.7, SalesCount = 78, Category = "Vệ sinh laptop", CategoryId = 2 },
                new ServiceItem { Id = 12, Name = "Bảo trì hệ thống", Description = "Bảo trì và tối ưu hóa hệ thống máy tính", Price = 300000, Image = "/images/services/system-maintenance.jpg", Rating = 4.9, SalesCount = 34, Category = "Bảo trì hệ thống", CategoryId = 7 }
            };
        }
    }
}
