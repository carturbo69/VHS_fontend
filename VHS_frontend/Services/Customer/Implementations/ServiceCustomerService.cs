using VHS_frontend.Models.ServiceDTOs;
using VHS_frontend.Services.Customer.Interfaces;

namespace VHS_frontend.Services.Customer.Implementations
{
    public class ServiceCustomerService : IServiceCustomerService
    {
        private readonly HttpClient _httpClient;

        public ServiceCustomerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ListServiceHomePageDTOs>> GetAllServiceHomePageAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ListServiceHomePageDTOs>>(
                "api/Services/services-homepage"
            );

            return response ?? new List<ListServiceHomePageDTOs>();
        }
    }
}
