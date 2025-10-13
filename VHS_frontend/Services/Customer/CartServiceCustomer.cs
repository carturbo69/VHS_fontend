using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Customer.Models.CartItemDTOs;
using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;

namespace VHS_frontend.Services.Customer
{
    public class CartServiceCustomer
    {
        private readonly HttpClient _httpClient;

        public CartServiceCustomer(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Lấy các CartItem theo AccountId (đã login) từ API backend.
        /// </summary>
        public async Task<List<ReadCartItemDTOs>> GetCartItemsByAccountIdAsync(Guid accountId, string? jwtToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var url = $"api/carts/account/{accountId}/items";

            using var resp = await _httpClient.GetAsync(url);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<ReadCartItemDTOs>();
            }
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");
            }

            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var items = await JsonSerializer.DeserializeAsync<List<ReadCartItemDTOs>>(stream, options)
                        ?? new List<ReadCartItemDTOs>();

            return items;
        }

        /// <summary>
        /// Lấy tổng số dịch vụ trong giỏ hàng (CartItem Count) theo AccountId.
        /// </summary>
        public async Task<TotalCartItemDTOs> GetTotalCartItemAsync(Guid accountId, string? jwtToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var url = $"api/carts/account/{accountId}/total";

            using var resp = await _httpClient.GetAsync(url);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");
            }

            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var totalDto = await JsonSerializer.DeserializeAsync<TotalCartItemDTOs>(stream, options)
                           ?? new TotalCartItemDTOs { TotalServices = 0 };

            return totalDto;
        }

        public async Task<List<ReadServiceOptionDTOs>?> GetAllOptionsByServiceIdAsync(Guid serviceId)
        {
            try
            {
                // Gọi API từ backend
                var response = await _httpClient.GetAsync($"api/Services/{serviceId}/options-cart");

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize về List DTO
                    var options = await response.Content.ReadFromJsonAsync<List<ReadServiceOptionDTOs>>();
                    return options;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<ReadServiceOptionDTOs>(); // không có option
                }
                else
                {
                    throw new HttpRequestException($"Lỗi khi gọi API: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi lấy option: {ex.Message}");
                return null;
            }
        }
    }
}
