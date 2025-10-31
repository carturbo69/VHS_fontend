using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VHS_frontend.Models;
using VHS_frontend.Models.ServiceDTOs;
using VHS_frontend.Models.ServiceShop;
using VHS_frontend.Areas.Admin.Models.Category;
using VHS_frontend.Services.Customer.Interfaces;

namespace VHS_frontend.Services
{
    public class ServiceShopService
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceCustomerService _serviceCustomerService;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true // Đảm bảo map đúng properties dù có khác case
        };

        public ServiceShopService(HttpClient httpClient, IServiceCustomerService serviceCustomerService)
        {
            _httpClient = httpClient;
            _serviceCustomerService = serviceCustomerService;
        }

        public async Task<ServiceShopViewModel?> GetServiceShopViewModelAsync(Guid providerId, int? categoryId, Guid? tagId, string sortBy, int page)
        {
            try
            {
                // Lấy tất cả dịch vụ của provider
                var response = await _httpClient.GetAsync($"api/ServiceProvider/provider/{providerId}");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<List<ServiceProviderReadDTO>>>(_json);
                var providerServices = apiResponse?.data ?? new List<ServiceProviderReadDTO>();

                if (!providerServices.Any())
                {
                    return null;
                }

                // Lấy thông tin provider từ dịch vụ đầu tiên (hoặc từ API riêng nếu có)
                var firstService = providerServices.First();
                
                // Lấy thông tin provider từ service detail nếu có (hoặc dùng dữ liệu mock tạm)
                // Trong thực tế nên có API riêng để lấy thông tin provider
                var providerInfo = await GetProviderInfoAsync(providerId, firstService);

                // Lấy categories
                var allCategories = await _serviceCustomerService.GetAllAsync();
                
                // Lấy rating thực tế từ API services-homepage
                var allServicesFromHomepage = await _serviceCustomerService.GetAllServiceHomePageAsync();
                var servicesRatingMap = allServicesFromHomepage
                    .Where(s => s != null)
                    .ToDictionary(s => s.ServiceId, s => new { 
                        Rating = s.AverageRating, 
                        TotalReviews = s.TotalReviews,
                        SalesCount = 0 // Có thể lấy từ booking count nếu có API
                    });
                
                // Lấy tags từ category cho các service không có tags (từ certificate được duyệt)
                var categoryTagsMap = new Dictionary<Guid, List<TagDTO>>();
                var uniqueCategoryIds = providerServices.Select(s => s.CategoryId).Distinct().ToList();
                
                foreach (var catId in uniqueCategoryIds)
                {
                    try
                    {
                        var tagsResponse = await _httpClient.GetAsync($"api/tag?categoryId={catId}&includeDeleted=false");
                        if (tagsResponse.IsSuccessStatusCode)
                        {
                            var categoryTags = await tagsResponse.Content.ReadFromJsonAsync<List<TagDTO>>(_json);
                            if (categoryTags != null && categoryTags.Any())
                            {
                                categoryTagsMap[catId] = categoryTags.Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name) && !(t.IsDeleted ?? false)).ToList();
                            }
                        }
                    }
                    catch { /* Ignore errors */ }
                }
                
                // Map services to ServiceItem với rating thực tế và tags từ category nếu cần
                var allServiceItems = providerServices.Select(s => 
                {
                    var ratingInfo = servicesRatingMap.ContainsKey(s.ServiceId) 
                        ? servicesRatingMap[s.ServiceId] 
                        : null;
                    
                    // Nếu service không có tags, lấy từ category (certificate được duyệt)
                    if ((s.Tags == null || !s.Tags.Any()) && categoryTagsMap.ContainsKey(s.CategoryId))
                    {
                        s.Tags = categoryTagsMap[s.CategoryId];
                    }
                    
                    return MapToServiceItem(s, allCategories, ratingInfo);
                }).ToList();

                // Lọc theo category và tag nếu có
                var filteredServices = allServiceItems;
                if (categoryId.HasValue)
                {
                    filteredServices = allServiceItems.Where(s => s.CategoryId == categoryId.Value).ToList();
                }
                
                // Lọc theo tag nếu có (chỉ lấy services có tag này)
                if (tagId.HasValue)
                {
                    // Lấy tên tag từ categoryTagsMap để filter
                    string? tagName = null;
                    foreach (var catTags in categoryTagsMap.Values)
                    {
                        var tag = catTags.FirstOrDefault(t => t.TagId == tagId.Value);
                        if (tag != null && !string.IsNullOrWhiteSpace(tag.Name))
                        {
                            tagName = tag.Name;
                            break;
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        filteredServices = filteredServices.Where(s => 
                            s.Tags != null && s.Tags.Contains(tagName)).ToList();
                    }
                }

                // Áp dụng logic sorting theo yêu cầu
                filteredServices = ApplySortingLogic(filteredServices, sortBy);

                // Pagination - hiển thị tất cả nếu ít, phân trang nếu nhiều
                var pageSize = 50; // Tăng pageSize để hiển thị nhiều dịch vụ hơn
                var totalPages = Math.Max(1, (int)Math.Ceiling(filteredServices.Count / (double)pageSize));
                var paginatedServices = filteredServices.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Lấy bestselling services (top 6 theo rating và sales)
                var bestsellingServices = allServiceItems
                    .OrderByDescending(s => s.Rating)
                    .ThenByDescending(s => s.SalesCount)
                    .Take(6)
                    .ToList();

                // Tạo shop categories từ services - nhóm theo CategoryId (Guid)
                var categoryMap = new Dictionary<int, (string Name, int Count)>();
                foreach (var item in allServiceItems)
                {
                    if (!categoryMap.ContainsKey(item.CategoryId))
                    {
                        categoryMap[item.CategoryId] = (item.Category, 0);
                    }
                    var current = categoryMap[item.CategoryId];
                    categoryMap[item.CategoryId] = (current.Name, current.Count + 1);
                }

                var shopCategories = categoryMap.Select(kvp => new ServiceCategory
                {
                    Id = kvp.Key,
                    Name = kvp.Value.Name,
                    Icon = "/images/categories/default.png",
                    ServiceCount = kvp.Value.Count
                }).ToList();

                // Map TẤT CẢ categories từ allCategories, hiển thị số lượng services của provider trong mỗi category
                // Và lấy tags cho mỗi category (từ certificate được duyệt)
                var mappedCategories = allCategories
                    .Select(c => 
                    {
                        var matchingServices = allServiceItems.Where(s => 
                            s.Category.ToLower() == (c.Name ?? "").ToLower()).ToList();
                        var matchingService = matchingServices.FirstOrDefault();
                        
                        // Lấy tags của category này từ categoryTagsMap (đã load ở trên)
                        var categoryTags = new List<CategoryTag>();
                        if (categoryTagsMap.ContainsKey(c.CategoryId))
                        {
                            var tagsInCategory = categoryTagsMap[c.CategoryId];
                            foreach (var tag in tagsInCategory)
                            {
                                // Đếm số service có tag này thuộc category này
                                var servicesWithTag = allServiceItems.Where(s =>
                                    s.Category.ToLower() == (c.Name ?? "").ToLower() &&
                                    s.Tags != null && s.Tags.Contains(tag.Name)).ToList();
                                
                                if (servicesWithTag.Any())
                                {
                                    categoryTags.Add(new CategoryTag
                                    {
                                        TagId = tag.TagId,
                                        Name = tag.Name,
                                        ServiceCount = servicesWithTag.Count
                                    });
                                }
                            }
                        }
                        
                        return new ServiceCategory
                        {
                            Id = matchingService?.CategoryId ?? c.CategoryId.GetHashCode(),
                            Name = c.Name ?? "",
                            Icon = "/images/categories/default.png",
                            ServiceCount = matchingServices.Count,
                            Tags = categoryTags.OrderBy(t => t.Name).ToList()
                        };
                    })
                    .Where(c => c.ServiceCount > 0) // Chỉ hiển thị categories có services của provider này
                    .ToList();

                var viewModel = new ServiceShopViewModel
                {
                    ShopInfo = providerInfo,
                    BestsellingServices = bestsellingServices,
                    ShopCategories = shopCategories,
                    AllCategories = mappedCategories,
                    Services = paginatedServices,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    SelectedCategoryId = categoryId,
                    SelectedTagId = tagId,
                    SortBy = sortBy,
                    ProviderId = providerId
                };

                return viewModel;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<ShopInfo> GetProviderInfoAsync(Guid providerId, ServiceProviderReadDTO firstService)
        {
            // Lấy thông tin provider từ service detail
            try
            {
                // Lấy từ một service detail để lấy provider info
                var serviceDetailResponse = await _httpClient.GetAsync($"api/Services/{firstService.ServiceId}");
                if (serviceDetailResponse.IsSuccessStatusCode)
                {
                    var serviceDetail = await serviceDetailResponse.Content.ReadFromJsonAsync<ServiceDetailDTOs>(_json);
                    if (serviceDetail?.Provider != null)
                    {
                        var p = serviceDetail.Provider;
                        var images = !string.IsNullOrEmpty(p.Images) 
                            ? p.Images.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
                            : null;

                        // Tính tỷ lệ phản hồi từ reviews
                        double responseRate = 100.0; // Mặc định 100% như trang detail
                        int totalRatings = 0;
                        
                        if (serviceDetail.Reviews != null && serviceDetail.Reviews.Any())
                        {
                            totalRatings = serviceDetail.Reviews.Count;
                            var reviewsWithReply = serviceDetail.Reviews.Count(r => !string.IsNullOrWhiteSpace(r.Reply));
                            if (totalRatings > 0)
                            {
                                responseRate = Math.Round((reviewsWithReply * 100.0) / totalRatings, 1);
                                // Đảm bảo không bao giờ = 0, nếu không có reply thì vẫn hiển thị 100% như trang detail
                                if (responseRate == 0 && totalRatings > 0)
                                {
                                    responseRate = 100.0; // Như trang detail hiển thị
                                }
                            }
                        }

                        return new ShopInfo
                        {
                            Id = providerId.GetHashCode(),
                            Name = p.ProviderName ?? "Đối tác VHS",
                            Logo = images ?? "/images/VeSinh.jpg",
                            Status = p.Status == "Active" ? "Online" : "Offline",
                            LastOnline = "Gần đây",
                            TotalServices = p.TotalServices,
                            Following = 0, // Cần API riêng
                            Followers = 0, // Cần API riêng
                            ResponseRate = responseRate,
                            Rating = p.AverageRatingAllServices,
                            TotalRatings = totalRatings > 0 ? totalRatings : serviceDetail.TotalReviews,
                            JoinDate = p.JoinedAt?.ToString("MM/yyyy") ?? "—",
                            IsFollowed = false
                        };
                    }
                }
            }
            catch { }

            // Fallback
            return new ShopInfo
            {
                Id = providerId.GetHashCode(),
                Name = "Đối tác VHS",
                Logo = "/images/VeSinh.jpg",
                Status = "Online",
                LastOnline = "Gần đây",
                TotalServices = 0,
                Following = 0,
                Followers = 0,
                ResponseRate = 99.0,
                Rating = 4.5,
                TotalRatings = 0,
                JoinDate = "—",
                IsFollowed = false
            };
        }

        private ServiceItem MapToServiceItem(ServiceProviderReadDTO dto, List<CategoryDTO>? categories = null, object? ratingInfo = null)
        {
            var images = !string.IsNullOrEmpty(dto.Images)
                ? dto.Images.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
                : null;

            // Tìm categoryId từ categories list bằng cách match Name
            var mappedCategoryId = 0;
            if (categories != null)
            {
                var matchedCategory = categories.FirstOrDefault(c => 
                    (c.Name ?? "").ToLower() == (dto.CategoryName ?? "").ToLower());
                if (matchedCategory != null)
                {
                    mappedCategoryId = matchedCategory.CategoryId.GetHashCode();
                }
                else
                {
                    mappedCategoryId = dto.CategoryId.GetHashCode();
                }
            }
            else
            {
                mappedCategoryId = dto.CategoryId.GetHashCode();
            }

            // Lấy rating từ ratingInfo nếu có
            double rating = 0;
            int ratingCount = 0;
            int salesCount = 0;
            
            if (ratingInfo != null)
            {
                var ratingType = ratingInfo.GetType();
                var ratingProp = ratingType.GetProperty("Rating");
                var reviewsProp = ratingType.GetProperty("TotalReviews");
                var salesProp = ratingType.GetProperty("SalesCount");
                
                if (ratingProp != null)
                    rating = Convert.ToDouble(ratingProp.GetValue(ratingInfo) ?? 0);
                
                if (reviewsProp != null)
                    ratingCount = Convert.ToInt32(reviewsProp.GetValue(ratingInfo) ?? 0);
                
                if (salesProp != null)
                    salesCount = Convert.ToInt32(salesProp.GetValue(ratingInfo) ?? 0);
            }

            // Lấy tags từ dto - kiểm tra kỹ lưỡng
            var tags = new List<string>();
            if (dto.Tags != null && dto.Tags.Any())
            {
                foreach (var tag in dto.Tags)
                {
                    if (tag != null && !string.IsNullOrWhiteSpace(tag.Name) && !(tag.IsDeleted ?? false))
                    {
                        tags.Add(tag.Name);
                    }
                }
            }

            return new ServiceItem
            {
                Id = dto.ServiceId.GetHashCode(),
                ServiceId = dto.ServiceId,  // Lưu Guid để link
                Name = dto.Title,
                Description = dto.Description ?? "",
                Image = images ?? "/images/VeSinh.jpg",
                ImageUrl = images ?? "/images/VeSinh.jpg",
                Price = dto.Price,
                Rating = rating,
                RatingCount = ratingCount,
                SalesCount = salesCount,
                Category = dto.CategoryName ?? "",
                CategoryId = mappedCategoryId,
                Tags = tags,
                CreatedAt = dto.CreatedAt
            };
        }

        private List<ServiceItem> ApplySortingLogic(List<ServiceItem> services, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                // Phổ Biến: Hiển thị tất cả dịch vụ (không filter)
                "popular" => services.OrderByDescending(s => s.Rating).ThenByDescending(s => s.SalesCount).ToList(),
                
                // Mới Nhất: Chỉ lấy dịch vụ được tạo trong 1 tháng gần nhất, sort theo CreatedAt DESC
                "newest" => services
                    .Where(s => s.CreatedAt.HasValue && s.CreatedAt.Value >= DateTime.UtcNow.AddMonths(-1))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToList(),
                
                // Bán Chạy: Chỉ lấy dịch vụ có rating >= 4 sao, sort theo rating DESC rồi sales DESC
                "bestselling" => services
                    .Where(s => s.Rating >= 4.0)
                    .OrderByDescending(s => s.Rating)
                    .ThenByDescending(s => s.SalesCount)
                    .ToList(),
                
                // Giá: Từ thấp đến cao
                "price-asc" => services.OrderBy(s => s.Price).ToList(),
                
                // Giá: Từ cao đến thấp
                "price-desc" => services.OrderByDescending(s => s.Price).ToList(),
                
                // Giá: Default (từ thấp đến cao)
                "price" => services.OrderBy(s => s.Price).ToList(),
                
                // Default: Giống Phổ Biến
                _ => services.OrderByDescending(s => s.Rating).ThenByDescending(s => s.SalesCount).ToList()
            };
        }

        public async Task<ServiceItem?> GetServiceByIdAsync(int id)
        {
            // Implement nếu cần
            return await Task.FromResult<ServiceItem?>(null);
        }

        public async Task<List<ServiceItem>> GetBestsellingServicesAsync(Guid providerId)
        {
            var viewModel = await GetServiceShopViewModelAsync(providerId, null, null, "popular", 1);
            return viewModel?.BestsellingServices ?? new List<ServiceItem>();
        }

        public async Task<List<ServiceItem>> GetServicesByCategoryAsync(Guid providerId, int categoryId, string sortBy, int page)
        {
            var viewModel = await GetServiceShopViewModelAsync(providerId, categoryId, null, sortBy, page);
            return viewModel?.Services ?? new List<ServiceItem>();
        }
    }

    // DTO classes cho API response
    public class ApiResponseWrapper<T>
    {
        public bool success { get; set; }
        public T? data { get; set; }
    }

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

    public class TagDTO
    {
        public Guid TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
