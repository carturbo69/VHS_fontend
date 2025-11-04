using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Category;
using VHS_frontend.Models.ServiceDTOs;
using VHS_frontend.Services.Customer.Interfaces;
using static System.Net.WebRequestMethods;

namespace VHS_frontend.Services.Customer.Implementations
{
    public class ServiceCustomerService : IServiceCustomerService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public ServiceCustomerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CategoryDTO>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
        {
            var list = await _httpClient.GetFromJsonAsync<List<CategoryDTO>>(
                $"/api/category/category-homepage?includeDeleted={includeDeleted.ToString().ToLower()}",
                _json, ct);
            return list ?? new();
        }

        public async Task<IEnumerable<ListServiceHomePageDTOs>> GetAllServiceHomePageAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ListServiceHomePageDTOs>>(
                "api/Services/services-homepage"
            );

            return response ?? new List<ListServiceHomePageDTOs>();
        }

        public async Task<IEnumerable<ListServiceHomePageDTOs>> GetTop05HighestRatedServicesAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ListServiceHomePageDTOs>>(
                "api/Services/services-top05"
            );
            return response ?? new List<ListServiceHomePageDTOs>();
        }

        public async Task<ServiceDetailDTOs?> GetServiceDetailAsync(Guid serviceId)
        {
            try
            {
                var url = $"api/Services/{serviceId}";
                Console.WriteLine($"[ServiceCustomerService] GET {url}");
                var res = await _httpClient.GetAsync(url);
                Console.WriteLine($"[ServiceCustomerService] Status: {res.StatusCode}");
                if (!res.IsSuccessStatusCode)
                {
                    var err = await res.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ServiceCustomerService] Error body: {err}");
                    return null;
                }
                var dto = await res.Content.ReadFromJsonAsync<ServiceDetailDTOs>(_json);
                Console.WriteLine($"[ServiceCustomerService] Parsed DTO: {(dto != null ? "OK" : "NULL")}");
                return dto;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ServiceCustomerService] HttpRequestException: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServiceCustomerService] Exception: {ex.Message}");
                return null;
            }
        }

    }
}
