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
            PropertyNameCaseInsensitive = true
        };

        public ServiceShopService(HttpClient httpClient, IServiceCustomerService serviceCustomerService)
        {
            _httpClient = httpClient;
            _serviceCustomerService = serviceCustomerService;
        }

        /// <summary>
        /// Lấy ViewModel cho trang Service Shop với đầy đủ thông tin provider và services
        /// </summary>
        public async Task<ServiceShopViewModel?> GetServiceShopViewModelAsync(Guid providerId, int? categoryId, Guid? tagId, string sortBy, int page)
        {
            // Sử dụng cả Console và Debug để đảm bảo log được hiển thị
            Console.WriteLine($"[ServiceShopService] === GetServiceShopViewModelAsync START ===");
            Console.WriteLine($"[ServiceShopService] providerId: {providerId}");
            Console.WriteLine($"[ServiceShopService] categoryId: {categoryId}, tagId: {tagId}, sortBy: {sortBy}, page: {page}");
            System.Diagnostics.Debug.WriteLine($"=== GetServiceShopViewModelAsync START ===");
            System.Diagnostics.Debug.WriteLine($"providerId: {providerId}");
            System.Diagnostics.Debug.WriteLine($"categoryId: {categoryId}, tagId: {tagId}, sortBy: {sortBy}, page: {page}");
            
            try
            {
                // BƯỚC 1: Lấy tất cả services của provider từ API
                var providerServices = await GetProviderServicesAsync(providerId);
                System.Diagnostics.Debug.WriteLine($"Step 1 Complete: Found {providerServices.Count} services for provider {providerId}");

                // BƯỚC 2: Lấy thông tin chi tiết của provider
                var shopInfo = await GetShopInfoAsync(providerId, providerServices);
                System.Diagnostics.Debug.WriteLine($"Step 2 Complete: ShopInfo - Name: '{shopInfo.Name}', TotalServices: {shopInfo.TotalServices}, Rating: {shopInfo.Rating}");

                // BƯỚC 3: Lấy categories và tags
                var allCategories = await GetAllCategoriesAsync();
                System.Diagnostics.Debug.WriteLine($"Step 3 Complete: Found {allCategories.Count} categories");

                // BƯỚC 4: Lấy ratings và reviews từ homepage API (nhanh hơn)
                var servicesRatingMap = await GetServicesRatingMapAsync(providerId);
                System.Diagnostics.Debug.WriteLine($"Step 4 Complete: Rating map for {servicesRatingMap.Count} services");

                // BƯỚC 5: Lấy tags theo category
                var categoryTagsMap = await GetCategoryTagsMapAsync(providerServices);
                System.Diagnostics.Debug.WriteLine($"Step 5 Complete: Tags for {categoryTagsMap.Count} categories");

                // BƯỚC 6: Map services thành ServiceItems
                var allServiceItems = MapServicesToServiceItems(providerServices, allCategories, servicesRatingMap, categoryTagsMap);
                System.Diagnostics.Debug.WriteLine($"Step 6 Complete: Mapped {allServiceItems.Count} ServiceItems");

                // BƯỚC 7: Lọc, sort và phân trang
                var (filteredServices, totalPages) = ApplyFiltersAndPagination(allServiceItems, categoryId, tagId, categoryTagsMap, sortBy, page);
                System.Diagnostics.Debug.WriteLine($"Step 7 Complete: Filtered to {filteredServices.Count} services, {totalPages} pages");

                // BƯỚC 8: Tạo các collections cho view
                var bestsellingServices = GetBestsellingServices(allServiceItems);
                var shopCategories = BuildShopCategories(allServiceItems);
                var allCategoriesViewModel = BuildAllCategoriesViewModel(allCategories, allServiceItems, categoryTagsMap);

                // BƯỚC 9: Tạo và trả về ViewModel
                var viewModel = new ServiceShopViewModel
                {
                    ProviderId = providerId,
                    ShopInfo = shopInfo,
                    BestsellingServices = bestsellingServices,
                    ShopCategories = shopCategories,
                    AllCategories = allCategoriesViewModel,
                    Services = filteredServices,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    SelectedCategoryId = categoryId,
                    SelectedTagId = tagId,
                    SortBy = sortBy
                };

                System.Diagnostics.Debug.WriteLine($"=== GetServiceShopViewModelAsync END ===");
                System.Diagnostics.Debug.WriteLine($"ViewModel created with {filteredServices.Count} services, ShopInfo.Name: '{shopInfo.Name}'");
                
                return viewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in GetServiceShopViewModelAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Trả về ViewModel với fallback data thay vì null
                return CreateFallbackViewModel(providerId, categoryId, tagId, sortBy, page);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// BƯỚC 1: Lấy tất cả services của provider từ API
        /// </summary>
        private async Task<List<ServiceProviderReadDTO>> GetProviderServicesAsync(Guid providerId)
        {
            try
            {
                Console.WriteLine($"[GetProviderServicesAsync] START - providerId: {providerId}");
                Console.WriteLine($"[GetProviderServicesAsync] Calling API: api/ServiceProvider/provider/{providerId}");
                
                HttpResponseMessage? response = null;
                try
                {
                    response = await _httpClient.GetAsync($"api/ServiceProvider/provider/{providerId}");
                    Console.WriteLine($"[GetProviderServicesAsync] API call completed. Status: {response.StatusCode}");
                }
                catch (Exception httpEx)
                {
                    Console.WriteLine($"[GetProviderServicesAsync] EXCEPTION during API call: {httpEx.Message}");
                    Console.WriteLine($"[GetProviderServicesAsync] StackTrace: {httpEx.StackTrace}");
                    return new List<ServiceProviderReadDTO>();
                }
                
                if (response == null)
                {
                    Console.WriteLine($"[GetProviderServicesAsync] ERROR: Response is null");
                    return new List<ServiceProviderReadDTO>();
                }
                
                Console.WriteLine($"[GetProviderServicesAsync] API Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"API Response Status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetProviderServicesAsync] WARNING: API call failed. Status: {response.StatusCode}, Content: {errorContent.Substring(0, Math.Min(500, errorContent.Length))}");
                    System.Diagnostics.Debug.WriteLine($"WARNING: API call failed with status {response.StatusCode}");
                    return new List<ServiceProviderReadDTO>();
                }

                string? jsonContent = null;
                try
                {
                    jsonContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetProviderServicesAsync] Response content length: {jsonContent?.Length ?? 0}");
                }
                catch (Exception readEx)
                {
                    Console.WriteLine($"[GetProviderServicesAsync] EXCEPTION reading response content: {readEx.Message}");
                    return new List<ServiceProviderReadDTO>();
                }

                ApiResponseWrapper<List<ServiceProviderReadDTO>>? apiResponse = null;
                try
                {
                    apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<List<ServiceProviderReadDTO>>>(_json);
                    Console.WriteLine($"[GetProviderServicesAsync] JSON deserialized. Success: {apiResponse?.success ?? false}");
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"[GetProviderServicesAsync] EXCEPTION deserializing JSON: {jsonEx.Message}");
                    Console.WriteLine($"[GetProviderServicesAsync] JSON content preview: {jsonContent?.Substring(0, Math.Min(200, jsonContent?.Length ?? 0))}");
                    return new List<ServiceProviderReadDTO>();
                }
                
                var allServices = apiResponse?.data ?? new List<ServiceProviderReadDTO>();
                Console.WriteLine($"[GetProviderServicesAsync] API returned {allServices.Count} services");
                System.Diagnostics.Debug.WriteLine($"API returned {allServices.Count} services");
                
                if (allServices.Count > 0)
                {
                    Console.WriteLine($"[GetProviderServicesAsync] First service ProviderId: {allServices[0].ProviderId}, Expected: {providerId}, Match: {allServices[0].ProviderId == providerId}");
                }
                
                // Đảm bảo chỉ lấy services có ProviderId khớp
                var validServices = allServices
                    .Where(s => s != null && s.ProviderId == providerId)
                    .ToList();

                if (validServices.Count != allServices.Count)
                {
                    Console.WriteLine($"[GetProviderServicesAsync] WARNING: Filtered out {allServices.Count - validServices.Count} services with mismatched ProviderId");
                    System.Diagnostics.Debug.WriteLine($"WARNING: Filtered out {allServices.Count - validServices.Count} services with mismatched ProviderId");
                }

                // Log first few services for debugging
                foreach (var svc in validServices.Take(3))
                {
                    Console.WriteLine($"[GetProviderServicesAsync] Service: {svc.Title}, ProviderId: {svc.ProviderId}, Category: {svc.CategoryName}");
                    System.Diagnostics.Debug.WriteLine($"  Service: {svc.Title}, ProviderId: {svc.ProviderId}, Category: {svc.CategoryName}");
                }

                Console.WriteLine($"[GetProviderServicesAsync] END - Returning {validServices.Count} valid services");
                return validServices;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetProviderServicesAsync] FATAL EXCEPTION: {ex.Message}");
                Console.WriteLine($"[GetProviderServicesAsync] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"ERROR in GetProviderServicesAsync: {ex.Message}");
                return new List<ServiceProviderReadDTO>();
            }
        }

        /// <summary>
        /// BƯỚC 2: Lấy thông tin chi tiết của provider từ service details
        /// </summary>
        private async Task<ShopInfo> GetShopInfoAsync(Guid providerId, List<ServiceProviderReadDTO> providerServices)
        {
            System.Diagnostics.Debug.WriteLine($"GetShopInfoAsync: providerId={providerId}, servicesCount={providerServices.Count}");

            if (!providerServices.Any())
            {
                System.Diagnostics.Debug.WriteLine($"No services found, returning fallback ShopInfo");
                return CreateFallbackShopInfo(providerId, 0);
            }

            // Thử lấy từ service đầu tiên (ưu tiên service có ProviderId khớp)
            var servicesToTry = providerServices
                .Where(s => s.ProviderId == providerId)
                .Take(5)
                .ToList();

            if (!servicesToTry.Any())
            {
                servicesToTry = providerServices.Take(5).ToList();
            }

            foreach (var service in servicesToTry)
            {
                try
                {
                    Console.WriteLine($"[GetShopInfoAsync] Trying service {service.ServiceId} for provider info");
                    System.Diagnostics.Debug.WriteLine($"Trying service {service.ServiceId} for provider info");
                    
                    var serviceDetailResponse = await _httpClient.GetAsync($"api/Services/{service.ServiceId}");
                    Console.WriteLine($"[GetShopInfoAsync] Service detail API Response: {serviceDetailResponse.StatusCode}");
                    
                    if (!serviceDetailResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[GetShopInfoAsync] Service detail API failed, trying next service");
                        continue;
                    }

                    var serviceDetail = await serviceDetailResponse.Content.ReadFromJsonAsync<ServiceDetailDTOs>(_json);
                    if (serviceDetail == null)
                    {
                        Console.WriteLine($"[GetShopInfoAsync] Service detail is null, trying next service");
                        System.Diagnostics.Debug.WriteLine($"Service detail is null, trying next service");
                        continue;
                    }
                    
                    Console.WriteLine($"[GetShopInfoAsync] Service detail loaded. ProviderId: {serviceDetail.ProviderId}, Expected: {providerId}, Provider object: {(serviceDetail.Provider != null ? "EXISTS" : "NULL")}");

                    var provider = serviceDetail.Provider;
                    
                    // QUAN TRỌNG: Vì service đã nằm trong providerServices (đã được API filter theo providerId),
                    // NÊN CHẤP NHẬN provider info luôn nếu:
                    // 1. Service có trong list providerServices (đã filter đúng)
                    // 2. HOẶC serviceDetail.ProviderId khớp với providerId
                    // 3. HOẶC provider.ProviderId khớp với providerId
                    bool isServiceInList = providerServices.Any(s => s.ServiceId == service.ServiceId);
                    bool serviceDetailIdMatches = serviceDetail.ProviderId == providerId;
                    bool providerIdMatches = provider != null && provider.ProviderId == providerId;
                    bool hasProviderName = provider != null && !string.IsNullOrWhiteSpace(provider.ProviderName);
                    
                    // CHẤP NHẬN nếu service có trong list HOẶC có ProviderId khớp HOẶC có ProviderName
                    bool shouldAccept = isServiceInList || serviceDetailIdMatches || providerIdMatches || hasProviderName;
                    
                    System.Diagnostics.Debug.WriteLine($"Service {service.ServiceId}: Checking acceptance criteria");
                    System.Diagnostics.Debug.WriteLine($"  isServiceInList: {isServiceInList} (service is in providerServices)");
                    System.Diagnostics.Debug.WriteLine($"  serviceDetailIdMatches: {serviceDetailIdMatches} (serviceDetail.ProviderId={serviceDetail.ProviderId} == {providerId})");
                    System.Diagnostics.Debug.WriteLine($"  providerIdMatches: {providerIdMatches} (provider.ProviderId matches)");
                    System.Diagnostics.Debug.WriteLine($"  hasProviderName: {hasProviderName}");
                    System.Diagnostics.Debug.WriteLine($"  shouldAccept: {shouldAccept}");
                    
                    if (provider != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  provider.ProviderId: {provider.ProviderId}");
                        System.Diagnostics.Debug.WriteLine($"  provider.ProviderName: '{provider.ProviderName ?? "NULL"}'");
                        System.Diagnostics.Debug.WriteLine($"  provider.Status: '{provider.Status ?? "NULL"}'");
                        System.Diagnostics.Debug.WriteLine($"  provider.AverageRatingAllServices: {provider.AverageRatingAllServices}");
                        System.Diagnostics.Debug.WriteLine($"  provider.JoinedAt: {provider.JoinedAt}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  WARNING: Provider object is NULL");
                    }
                    
                    if (shouldAccept)
                    {
                        // Lấy tên provider - ưu tiên từ Provider object
                        string providerName = "Đối tác VHS";
                        if (provider != null && !string.IsNullOrWhiteSpace(provider.ProviderName))
                        {
                            providerName = provider.ProviderName;
                        }
                        else if (provider == null)
                        {
                            // Nếu không có Provider object, giữ tên mặc định
                            providerName = "Đối tác VHS";
                        }
                        
                        Console.WriteLine($"[GetShopInfoAsync] SUCCESS: Accepting provider info - Name: '{providerName}'");
                        System.Diagnostics.Debug.WriteLine($"SUCCESS: Accepting provider info - Name: '{providerName}'");
                        
                        // Tính response rate từ reviews
                        double responseRate = 100.0;
                        int totalRatings = 0;
                        
                        if (serviceDetail.Reviews != null && serviceDetail.Reviews.Any())
                        {
                            totalRatings = serviceDetail.Reviews.Count;
                            var reviewsWithReply = serviceDetail.Reviews.Count(r => !string.IsNullOrWhiteSpace(r.Reply));
                            if (totalRatings > 0)
                            {
                                responseRate = Math.Round((reviewsWithReply * 100.0) / totalRatings, 1);
                                if (responseRate == 0 && totalRatings > 0)
                                {
                                    responseRate = 100.0;
                                }
                            }
                        }

                        // Lấy logo từ images
                        var logo = "/images/VeSinh.jpg";
                        if (provider != null && !string.IsNullOrEmpty(provider.Images))
                        {
                            var images = provider.Images.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            if (images.Length > 0)
                            {
                                logo = images[0].Trim();
                            }
                        }

                        // Lấy rating từ provider hoặc mặc định 0
                        // Lưu ý: Rating sẽ được tính lại trong EnhanceShopInfoWithAllServices từ tất cả services
                        double rating = 0;
                        if (provider != null && provider.AverageRatingAllServices > 0)
                        {
                            rating = provider.AverageRatingAllServices; // Giá trị tạm, sẽ được cập nhật trong EnhanceShopInfoWithAllServices
                        }

                        // Lấy status
                        string status = "Online";
                        if (provider != null)
                        {
                            status = (provider.Status == "Active" || provider.Status == "Approved") ? "Online" : "Offline";
                        }

                        // Lấy join date
                        string joinDate = "—";
                        if (provider != null && provider.JoinedAt.HasValue)
                        {
                            joinDate = provider.JoinedAt.Value.ToString("MM/yyyy");
                        }

                        var shopInfo = new ShopInfo
                        {
                            Id = providerId.GetHashCode(),
                            Name = providerName,
                            Logo = logo,
                            Status = status,
                            LastOnline = "Gần đây",
                            TotalServices = providerServices.Count,
                            Following = 0,
                            Followers = 0,
                            ResponseRate = responseRate,
                            Rating = rating,
                            TotalRatings = totalRatings > 0 ? totalRatings : serviceDetail.TotalReviews,
                            JoinDate = joinDate,
                            IsFollowed = false
                        };

                        // Enhance với ratings từ tất cả services
                        await EnhanceShopInfoWithAllServices(shopInfo, providerId, providerServices);
                        
                        System.Diagnostics.Debug.WriteLine($"Final ShopInfo: Name='{shopInfo.Name}', Rating={shopInfo.Rating}, TotalRatings={shopInfo.TotalRatings}, TotalServices={shopInfo.TotalServices}");
                        
                        return shopInfo;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"REJECTING: shouldAccept=false");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR getting service detail {service.ServiceId}: {ex.Message}");
                    continue;
                }
            }

            // Fallback nếu không lấy được từ service details
            System.Diagnostics.Debug.WriteLine($"WARNING: Could not get provider info from service details, using fallback");
            return CreateFallbackShopInfo(providerId, providerServices.Count);
        }

        /// <summary>
        /// Enhance ShopInfo với ratings và reviews từ tất cả services
        /// </summary>
        private async Task EnhanceShopInfoWithAllServices(ShopInfo shopInfo, Guid providerId, List<ServiceProviderReadDTO> providerServices)
        {
            try
            {
                Console.WriteLine($"[EnhanceShopInfoWithAllServices] START - providerId: {providerId}, servicesCount: {providerServices.Count}");
                
                // Tính rating trung bình có trọng số từ homepage API (weighted average)
                double totalWeightedRating = 0;
                int totalReviewsCount = 0;
                var allRatings = new List<double>();

                try
                {
                    var allServicesFromHomepage = await _serviceCustomerService.GetAllServiceHomePageAsync();
                    if (allServicesFromHomepage != null)
                    {
                        var providerServicesFromHomepage = allServicesFromHomepage
                            .Where(s => s != null && s.ProviderId == providerId)
                            .ToList();

                        Console.WriteLine($"[EnhanceShopInfoWithAllServices] Found {providerServicesFromHomepage.Count} services from homepage API");

                        foreach (var svc in providerServicesFromHomepage)
                        {
                            if (svc.AverageRating > 0 && svc.TotalReviews > 0)
                            {
                                // Tính weighted average: tổng (rating * số reviews) / tổng số reviews
                                totalWeightedRating += svc.AverageRating * svc.TotalReviews;
                                totalReviewsCount += svc.TotalReviews;
                                Console.WriteLine($"[EnhanceShopInfoWithAllServices] Service {svc.ServiceId}: Rating={svc.AverageRating}, Reviews={svc.TotalReviews}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EnhanceShopInfoWithAllServices] Error getting homepage services: {ex.Message}");
                }

                // Nếu chưa có đủ từ homepage, lấy từ service details để có rating thực tế
                if (totalReviewsCount == 0 && providerServices.Any())
                {
                    Console.WriteLine($"[EnhanceShopInfoWithAllServices] No data from homepage, getting from service details");
                    var servicesToCheck = providerServices.Take(5).ToList();
                    foreach (var service in servicesToCheck)
                    {
                        try
                        {
                            var serviceDetailResponse = await _httpClient.GetAsync($"api/Services/{service.ServiceId}");
                            if (serviceDetailResponse.IsSuccessStatusCode)
                            {
                                var serviceDetail = await serviceDetailResponse.Content.ReadFromJsonAsync<ServiceDetailDTOs>(_json);
                                if (serviceDetail?.Provider != null && serviceDetail.Provider.ProviderId == providerId)
                                {
                                    if (serviceDetail.Reviews != null && serviceDetail.Reviews.Any())
                                    {
                                        foreach (var review in serviceDetail.Reviews)
                                        {
                                            if (review.Rating.HasValue && review.Rating.Value > 0)
                                            {
                                                allRatings.Add(review.Rating.Value);
                                                totalReviewsCount++;
                                            }
                                        }
                                        Console.WriteLine($"[EnhanceShopInfoWithAllServices] Service {service.ServiceId}: Added {serviceDetail.Reviews.Count(r => r.Rating.HasValue && r.Rating.Value > 0)} ratings");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[EnhanceShopInfoWithAllServices] Error getting service detail {service.ServiceId}: {ex.Message}");
                        }
                    }
                }

                // Cập nhật rating
                if (totalReviewsCount > 0)
                {
                    double averageRating = 0;
                    
                    // Nếu có weighted rating từ homepage
                    if (totalWeightedRating > 0)
                    {
                        averageRating = totalWeightedRating / totalReviewsCount;
                        Console.WriteLine($"[EnhanceShopInfoWithAllServices] Calculated weighted average: {averageRating} from {totalReviewsCount} reviews");
                    }
                    // Hoặc tính từ actual ratings
                    else if (allRatings.Any())
                    {
                        averageRating = allRatings.Average();
                        Console.WriteLine($"[EnhanceShopInfoWithAllServices] Calculated average from actual ratings: {averageRating} from {allRatings.Count} ratings");
                    }
                    
                    shopInfo.Rating = Math.Round(averageRating, 1);
                    shopInfo.TotalRatings = totalReviewsCount;
                    Console.WriteLine($"[EnhanceShopInfoWithAllServices] Final Rating: {shopInfo.Rating}, TotalRatings: {shopInfo.TotalRatings}");
                }
                else
                {
                    // Nếu không có đánh giá nào, set rating = 0
                    shopInfo.Rating = 0;
                    shopInfo.TotalRatings = 0;
                    Console.WriteLine($"[EnhanceShopInfoWithAllServices] No reviews found, setting Rating to 0");
                }

                // Tính response rate
                if (totalReviewsCount > 0)
                {
                    int reviewsWithReplyCount = 0;
                    var servicesToCheck = providerServices.Take(5).ToList();
                    foreach (var service in servicesToCheck)
                    {
                        try
                        {
                            var serviceDetailResponse = await _httpClient.GetAsync($"api/Services/{service.ServiceId}");
                            if (serviceDetailResponse.IsSuccessStatusCode)
                            {
                                var serviceDetail = await serviceDetailResponse.Content.ReadFromJsonAsync<ServiceDetailDTOs>(_json);
                                if (serviceDetail?.Reviews != null)
                                {
                                    reviewsWithReplyCount += serviceDetail.Reviews.Count(r => !string.IsNullOrWhiteSpace(r.Reply));
                                }
                            }
                        }
                        catch { }
                    }

                    if (reviewsWithReplyCount > 0)
                    {
                        shopInfo.ResponseRate = Math.Round((reviewsWithReplyCount * 100.0) / totalReviewsCount, 1);
                        if (shopInfo.ResponseRate == 0)
                        {
                            shopInfo.ResponseRate = 100.0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to enhance shop info: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy tất cả categories
        /// </summary>
        private async Task<List<CategoryDTO>> GetAllCategoriesAsync()
        {
            try
            {
                return await _serviceCustomerService.GetAllAsync() ?? new List<CategoryDTO>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to get categories: {ex.Message}");
                return new List<CategoryDTO>();
            }
        }

        /// <summary>
        /// Lấy rating map từ homepage API
        /// </summary>
        private async Task<Dictionary<Guid, object>> GetServicesRatingMapAsync(Guid providerId)
        {
            var ratingMap = new Dictionary<Guid, object>();
            try
            {
                var allServicesFromHomepage = await _serviceCustomerService.GetAllServiceHomePageAsync();
                if (allServicesFromHomepage != null)
                {
                    var providerServices = allServicesFromHomepage
                        .Where(s => s != null && s.ProviderId == providerId)
                        .ToList();

                    foreach (var svc in providerServices)
                    {
                        ratingMap[svc.ServiceId] = new
                        {
                            Rating = svc.AverageRating,
                            TotalReviews = svc.TotalReviews,
                            SalesCount = 0
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to get rating map: {ex.Message}");
            }
            return ratingMap;
        }

        /// <summary>
        /// Lấy tags theo category
        /// </summary>
        private async Task<Dictionary<Guid, List<TagDTO>>> GetCategoryTagsMapAsync(List<ServiceProviderReadDTO> providerServices)
        {
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
                            categoryTagsMap[catId] = categoryTags
                                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name) && !(t.IsDeleted ?? false))
                                .ToList();
                        }
                    }
                }
                catch { }
            }

            return categoryTagsMap;
        }

        /// <summary>
        /// Map services thành ServiceItems
        /// </summary>
        private List<ServiceItem> MapServicesToServiceItems(
            List<ServiceProviderReadDTO> providerServices,
            List<CategoryDTO> allCategories,
            Dictionary<Guid, object> servicesRatingMap,
            Dictionary<Guid, List<TagDTO>> categoryTagsMap)
        {
            var serviceItems = new List<ServiceItem>();

            foreach (var dto in providerServices)
            {
                try
                {
                    // Lấy rating info
                    var ratingInfo = servicesRatingMap.ContainsKey(dto.ServiceId) ? servicesRatingMap[dto.ServiceId] : null;

                    // Nếu service không có tags, lấy từ category
                    if ((dto.Tags == null || !dto.Tags.Any()) && categoryTagsMap.ContainsKey(dto.CategoryId))
                    {
                        dto.Tags = categoryTagsMap[dto.CategoryId];
                    }

                    var serviceItem = MapToServiceItem(dto, allCategories, ratingInfo);
                    serviceItems.Add(serviceItem);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR mapping service {dto.ServiceId}: {ex.Message}");
                }
            }

            return serviceItems;
        }

        /// <summary>
        /// Map một ServiceProviderReadDTO thành ServiceItem
        /// </summary>
        private ServiceItem MapToServiceItem(ServiceProviderReadDTO dto, List<CategoryDTO>? categories = null, object? ratingInfo = null)
        {
            // Parse ảnh từ dto.Images (có thể là JSON array hoặc comma-separated string)
            string? images = null;
            if (!string.IsNullOrWhiteSpace(dto.Images))
            {
                try
                {
                    // Thử parse JSON array trước
                    var arr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(dto.Images);
                    if (arr != null && arr.Count > 0)
                    {
                        images = arr[0].Trim();
                    }
                }
                catch
                {
                    // Nếu không phải JSON, thử split bằng dấu phẩy
                    if (dto.Images.Contains(','))
                    {
                        images = dto.Images.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                           .FirstOrDefault()?.Trim();
                    }
                    else
                    {
                        // Nếu không có dấu phẩy, dùng nguyên chuỗi
                        images = dto.Images.Trim();
                    }
                }
            }

            // Map categoryId
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

            // Lấy rating từ ratingInfo
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

            // Lấy tags
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
                ServiceId = dto.ServiceId,
                Name = dto.Title,
                Description = dto.Description ?? "",
                Image = images ?? "/images/VeSinh.jpg",
                ImageUrl = images ?? "/images/VeSinh.jpg",
                Unit = dto.UnitType ?? "/giờ", // Map UnitType từ DTO sang Unit
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

        /// <summary>
        /// Áp dụng filters, sorting và pagination
        /// </summary>
        private (List<ServiceItem> filteredServices, int totalPages) ApplyFiltersAndPagination(
            List<ServiceItem> allServiceItems,
            int? categoryId,
            Guid? tagId,
            Dictionary<Guid, List<TagDTO>> categoryTagsMap,
            string sortBy,
            int page)
        {
            var filteredServices = allServiceItems;

            // Filter by category
            if (categoryId.HasValue)
            {
                filteredServices = filteredServices.Where(s => s.CategoryId == categoryId.Value).ToList();
            }

            // Filter by tag
            if (tagId.HasValue)
            {
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

            // Apply sorting
            filteredServices = ApplySortingLogic(filteredServices, sortBy);

            // Pagination
            var pageSize = 50;
            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredServices.Count / (double)pageSize));
            var paginatedServices = filteredServices.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return (paginatedServices, totalPages);
        }

        /// <summary>
        /// Áp dụng sorting logic
        /// </summary>
        private List<ServiceItem> ApplySortingLogic(List<ServiceItem> services, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "popular" => services.OrderByDescending(s => s.Rating).ThenByDescending(s => s.SalesCount).ToList(),
                "newest" => services
                    .Where(s => s.CreatedAt.HasValue && s.CreatedAt.Value >= DateTime.UtcNow.AddMonths(-1))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToList(),
                "bestselling" => services
                    .Where(s => s.Rating >= 4.0)
                    .OrderByDescending(s => s.Rating)
                    .ThenByDescending(s => s.SalesCount)
                    .ToList(),
                "price-asc" => services.OrderBy(s => s.Price).ToList(),
                "price-desc" => services.OrderByDescending(s => s.Price).ToList(),
                "price" => services.OrderBy(s => s.Price).ToList(),
                _ => services.OrderByDescending(s => s.Rating).ThenByDescending(s => s.SalesCount).ToList()
            };
        }

        /// <summary>
        /// Lấy bestselling services
        /// </summary>
        private List<ServiceItem> GetBestsellingServices(List<ServiceItem> allServiceItems)
        {
            return allServiceItems
                .OrderByDescending(s => s.Rating)
                .ThenByDescending(s => s.SalesCount)
                .Take(6)
                .ToList();
        }

        /// <summary>
        /// Build shop categories từ services
        /// </summary>
        private List<ServiceCategory> BuildShopCategories(List<ServiceItem> allServiceItems)
        {
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

            return categoryMap.Select(kvp => new ServiceCategory
            {
                Id = kvp.Key,
                Name = kvp.Value.Name,
                Icon = "/images/categories/default.png",
                ServiceCount = kvp.Value.Count
            }).ToList();
        }

        /// <summary>
        /// Build all categories view model
        /// </summary>
        private List<ServiceCategory> BuildAllCategoriesViewModel(
            List<CategoryDTO> allCategories,
            List<ServiceItem> allServiceItems,
            Dictionary<Guid, List<TagDTO>> categoryTagsMap)
        {
            return allCategories
                .Select(c =>
                {
                    var matchingServices = allServiceItems.Where(s =>
                        s.Category.ToLower() == (c.Name ?? "").ToLower()).ToList();

                    var categoryTags = new List<CategoryTag>();
                    if (categoryTagsMap.ContainsKey(c.CategoryId))
                    {
                        var tagsInCategory = categoryTagsMap[c.CategoryId];
                        foreach (var tag in tagsInCategory)
                        {
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

                    var matchingService = matchingServices.FirstOrDefault();
                    return new ServiceCategory
                    {
                        Id = matchingService?.CategoryId ?? c.CategoryId.GetHashCode(),
                        Name = c.Name ?? "",
                        Icon = "/images/categories/default.png",
                        ServiceCount = matchingServices.Count,
                        Tags = categoryTags.OrderBy(t => t.Name).ToList()
                    };
                })
                .Where(c => c.ServiceCount > 0)
                .ToList();
        }

        /// <summary>
        /// Tạo fallback ShopInfo
        /// </summary>
        private ShopInfo CreateFallbackShopInfo(Guid providerId, int serviceCount)
        {
            return new ShopInfo
            {
                Id = providerId.GetHashCode(),
                Name = "Đối tác VHS",
                Logo = "/images/VeSinh.jpg",
                Status = "Online",
                LastOnline = "Gần đây",
                TotalServices = serviceCount,
                Following = 0,
                Followers = 0,
                ResponseRate = 100.0,
                Rating = 0, // Đảm bảo rating = 0 khi không có đánh giá
                TotalRatings = 0, // Đảm bảo TotalRatings = 0 khi không có đánh giá
                JoinDate = "—",
                IsFollowed = false
            };
        }

        /// <summary>
        /// Tạo fallback ViewModel
        /// </summary>
        private ServiceShopViewModel CreateFallbackViewModel(Guid providerId, int? categoryId, Guid? tagId, string sortBy, int page)
        {
            return new ServiceShopViewModel
            {
                ProviderId = providerId,
                ShopInfo = CreateFallbackShopInfo(providerId, 0),
                BestsellingServices = new List<ServiceItem>(),
                ShopCategories = new List<ServiceCategory>(),
                AllCategories = new List<ServiceCategory>(),
                Services = new List<ServiceItem>(),
                CurrentPage = page,
                TotalPages = 1,
                SelectedCategoryId = categoryId,
                SelectedTagId = tagId,
                SortBy = sortBy
            };
        }

        #endregion

        #region Public Methods

        public async Task<ServiceItem?> GetServiceByIdAsync(int id)
        {
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

        #endregion
    }
}

