using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using VHS_frontend.Models.ServiceDTOs;
using VHS_frontend.Models.ServiceShop;
using VHS_frontend.Services.Customer.Implementations;
using VHS_frontend.Services.Customer.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace VHS_frontend.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IServiceCustomerService _serviceCustomerService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;

        public ServicesController(IServiceCustomerService serviceCustomerService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _serviceCustomerService = serviceCustomerService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
        
        private HttpClient GetHttpClient()
        {
            var client = _httpClientFactory.CreateClient();
            var backendBase = _configuration["Apis:Backend"];
            if (!string.IsNullOrWhiteSpace(backendBase))
            {
                client.BaseAddress = new Uri(backendBase.TrimEnd('/'));
            }
            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
         int page = 1,
         string? search = null,
         string? category = null,
         string? tag = null,
         string? sort = null,
         string? filter = null,
         string? provider = null)
        {
            const int pageSize = 20;

            var dtos = await _serviceCustomerService.GetAllServiceHomePageAsync();

            var categories = await _serviceCustomerService.GetAllAsync();
            ViewBag.Categories = categories;
            
            // Lấy danh sách providers từ tất cả services (trước khi filter)
            var allProviders = dtos
                .Where(s => s.ProviderId != Guid.Empty && !string.IsNullOrWhiteSpace(s.ProviderName))
                .Select(s => new { s.ProviderId, s.ProviderName })
                .Distinct()
                .OrderBy(p => p.ProviderName)
                .ToList();
            ViewBag.Providers = allProviders;

            // Lấy tags theo category đã chọn (nếu có)
            List<TagDTO>? tags = null;
            if (!string.IsNullOrWhiteSpace(category) && Guid.TryParse(category, out var selectedCategoryId))
            {
                try
                {
                    var httpClient = GetHttpClient();
                    var tagsResponse = await httpClient.GetAsync($"api/tag?categoryId={selectedCategoryId}&includeDeleted=false");
                    if (tagsResponse.IsSuccessStatusCode)
                    {
                        tags = await tagsResponse.Content.ReadFromJsonAsync<List<TagDTO>>(_jsonOptions);
                        tags = tags?.Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name) && !(t.IsDeleted ?? false)).ToList();
                    }
                }
                catch { /* ignore errors */ }
            }

            ViewBag.Tags = tags ?? new List<TagDTO>();

            var all = dtos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(s =>
                    (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.Description) && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(category, out var categoryId))
                {
                    all = all.Where(s => s.CategoryId == categoryId);
                }
                else
                {
                    all = all.Where(s =>
                        !string.IsNullOrWhiteSpace(s.CategoryName) &&
                        s.CategoryName.Equals(category, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Filter theo tag (nếu có)
            if (!string.IsNullOrWhiteSpace(tag) && Guid.TryParse(tag, out var tagId))
            {
                string? tagName = null;
                
                // Ưu tiên: Lấy tagName từ tags list đã load (từ category đã chọn)
                if (tags != null)
                {
                    var selectedTag = tags.FirstOrDefault(t => t.TagId == tagId);
                    if (selectedTag != null && !string.IsNullOrWhiteSpace(selectedTag.Name))
                    {
                        tagName = selectedTag.Name;
                    }
                }

                // Nếu không tìm thấy trong tags đã load, gọi API để lấy tag info
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    try
                    {
                        var httpClient = GetHttpClient();
                        // Thử lấy tag từ category đã chọn
                        if (!string.IsNullOrWhiteSpace(category) && Guid.TryParse(category, out var catId))
                        {
                            var tagsResponse = await httpClient.GetAsync($"api/tag?categoryId={catId}&includeDeleted=false");
                            if (tagsResponse.IsSuccessStatusCode)
                            {
                                var allTags = await tagsResponse.Content.ReadFromJsonAsync<List<TagDTO>>(_jsonOptions);
                                var foundTag = allTags?.FirstOrDefault(t => t.TagId == tagId);
                                if (foundTag != null && !string.IsNullOrWhiteSpace(foundTag.Name))
                                {
                                    tagName = foundTag.Name;
                                }
                            }
                        }
                        
                        // Nếu vẫn không tìm thấy, thử lấy tag trực tiếp từ tagId
                        if (string.IsNullOrWhiteSpace(tagName))
                        {
                            var tagResponse = await httpClient.GetAsync($"api/tag/{tagId}");
                            if (tagResponse.IsSuccessStatusCode)
                            {
                                var tagInfo = await tagResponse.Content.ReadFromJsonAsync<TagDTO>(_jsonOptions);
                                if (tagInfo != null && !string.IsNullOrWhiteSpace(tagInfo.Name))
                                {
                                    tagName = tagInfo.Name;
                                }
                            }
                        }
                    }
                    catch { /* ignore errors */ }
                }

                // Filter services theo tagName (kiểm tra trong title hoặc description)
                // Lưu ý: Đây là cách filter tạm thời vì DTO không có thông tin Tags
                // Để filter chính xác, cần có API backend filter services theo tagId
                if (!string.IsNullOrWhiteSpace(tagName))
                {
                    all = all.Where(s =>
                        (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(tagName, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(s.Description) && s.Description.Contains(tagName, StringComparison.OrdinalIgnoreCase))
                    );
                }
                else
                {
                    // Nếu không tìm thấy tagName, không filter (hiển thị tất cả services của category)
                    // Hoặc có thể return empty nếu muốn strict filter
                }
            }

            // Filter theo provider (nếu có)
            if (!string.IsNullOrWhiteSpace(provider) && Guid.TryParse(provider, out var providerId))
            {
                all = all.Where(s => s.ProviderId == providerId);
            }

            // Filter theo khoảng giá (sort parameter)
            all = sort switch
            {
                "price-500" => all.Where(s => s.Price < 500_000),
                "price-500-1000" => all.Where(s => s.Price >= 500_000 && s.Price < 1_000_000),
                "price-1000-5000" => all.Where(s => s.Price >= 1_000_000 && s.Price <= 5_000_000),
                "price-5000" => all.Where(s => s.Price > 5_000_000),
                _ => all
            };

            // Sắp xếp kết quả (filter parameter)
            all = filter switch
            {
                "price-asc" => all.OrderBy(s => s.Price),
                "price-desc" => all.OrderByDescending(s => s.Price),
                "stars" => all.Where(s => s.AverageRating >= 4.0)
                              .OrderByDescending(s => s.AverageRating)
                              .ThenByDescending(s => s.TotalReviews),
                _ => all.OrderBy(s => s.Title)
            };

            // Xáo trộn ngẫu nhiên danh sách dịch vụ (chỉ khi không có filter sắp xếp cụ thể)
            if (string.IsNullOrWhiteSpace(filter))
            {
                var allList = all.ToList();
                // Sử dụng Random với seed dựa trên thời gian để mỗi lần reload có thứ tự khác nhau
                var random = new Random((int)DateTime.Now.Ticks);
                for (int i = allList.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = allList[i];
                    allList[i] = allList[j];
                    allList[j] = temp;
                }
                all = allList.AsQueryable();
            }

            page = Math.Max(1, page);
            var totalFiltered = all.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)pageSize));

            var models = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Tag = tag;
            ViewBag.Sort = sort;
            ViewBag.Filter = filter;
            ViewBag.Provider = provider;

            return View(models);
        }


        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            Console.WriteLine($"[ServicesController] Details requested: id={id}");
            ServiceDetailDTOs? dto = await _serviceCustomerService.GetServiceDetailAsync(id);
            if (dto is null)
            {
                Console.WriteLine($"[ServicesController] Service not found: id={id}. Redirecting to Index.");
                TempData["Error"] = "Không tìm thấy dịch vụ hoặc dịch vụ đã bị gỡ.";
                return RedirectToAction(nameof(Index));
            }

            var top = await _serviceCustomerService.GetTop05HighestRatedServicesAsync();
            ViewBag.TopRight = top?.Take(4) ?? Enumerable.Empty<ListServiceHomePageDTOs>();

            var all = await _serviceCustomerService.GetAllServiceHomePageAsync();
            ViewBag.RelatedServices = all?
                .Where(s => s != null && s.ServiceId != dto.ServiceId)
                .Take(40)
                .ToList()
                ?? new System.Collections.Generic.List<ListServiceHomePageDTOs>();

            // Tính số dịch vụ đã duyệt của provider (chỉ đếm Approved/Active, không đếm Pending)
            var providerIdToUse = dto.ProviderId != Guid.Empty ? dto.ProviderId : (dto.Provider?.ProviderId ?? Guid.Empty);
            if (providerIdToUse != Guid.Empty)
            {
                var approvedServicesCount = all?
                    .Where(s => s != null && s.ProviderId == providerIdToUse)
                    .Count() ?? 0;
                ViewBag.ApprovedServicesCount = approvedServicesCount;
            }
            else
            {
                ViewBag.ApprovedServicesCount = dto.Provider?.TotalServices ?? 0;
            }

            return View(dto);
        }

        /// <summary>
        /// API endpoint: Lấy tags theo category
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTagsByCategory(string categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId) || !Guid.TryParse(categoryId, out var catId))
            {
                return Json(new List<TagDTO>());
            }

            try
            {
                var httpClient = GetHttpClient();
                var tagsResponse = await httpClient.GetAsync($"api/tag?categoryId={catId}&includeDeleted=false");
                if (tagsResponse.IsSuccessStatusCode)
                {
                    var tags = await tagsResponse.Content.ReadFromJsonAsync<List<TagDTO>>(_jsonOptions);
                    tags = tags?.Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name) && !(t.IsDeleted ?? false)).ToList();
                    return Json(tags ?? new List<TagDTO>());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicesController] Error getting tags: {ex.Message}");
            }

            return Json(new List<TagDTO>());
        }

    }
}
