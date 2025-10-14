using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Customer.Models.CartItemDTOs;
using VHS_frontend.Areas.Customer.Models.ServiceOptionDTOs;
using VHS_frontend.Areas.Customer.Models.VoucherDTOs;
using System.Net.Http.Json;

namespace VHS_frontend.Services.Customer
{
    public class CartServiceCustomer
    {
        private readonly HttpClient _httpClient;

        public CartServiceCustomer(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private void SetAuthHeader(string? jwtToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }
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

        public async Task<List<ReadVoucherByCustomerDTOs>> GetCartVouchersAsync(string? jwtToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var url = "api/carts/cart-vouchers";

            using var resp = await _httpClient.GetAsync(url);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return new List<ReadVoucherByCustomerDTOs>();

            resp.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var vouchers = await resp.Content.ReadFromJsonAsync<List<ReadVoucherByCustomerDTOs>>(options)
                           ?? new List<ReadVoucherByCustomerDTOs>();

            return vouchers;
        }

        /// <summary>
        /// Gọi API backend: POST api/carts/addtocart-items?accountId=...  (body: AddCartItemRequest)
        /// Backend trả về bool. true = thành công.
        /// </summary>
        public async Task<bool> AddItemToCartAsync(Guid accountId, AddCartItemRequest request, string? jwtToken)
        {
            // Header Authorization (nếu có JWT)
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var url = $"api/carts/addtocart-items?accountId={accountId}";

            using var resp = await _httpClient.PostAsJsonAsync(url, request);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            // Nếu backend quy ước luôn trả 200 OK + bool trong body:
            // -> sẽ vào EnsureSuccessStatusCode, rồi đọc bool.
            // Nếu backend có thể trả 400 khi lỗi, dòng dưới sẽ throw.
            resp.EnsureSuccessStatusCode();

            // Đọc bool từ body
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            bool? success = await resp.Content.ReadFromJsonAsync<bool?>(options);
            return success.GetValueOrDefault();
        }

        /// <summary>
        /// Gọi API backend để xóa toàn bộ cart items của một account.
        /// DELETE: api/carts/account/{accountId}/items:clear
        /// </summary>
        public async Task<bool> ClearCartAsync(Guid accountId, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/carts/account/{accountId}/items:clear";
            using var resp = await _httpClient.DeleteAsync(url);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            if (resp.IsSuccessStatusCode)
                return true;

            return false;
        }

        /// <summary>
        /// Gọi API backend để xóa một cart item theo cartItemId.
        /// DELETE: api/carts/account/{accountId}/items/{cartItemId}
        /// </summary>
        public async Task<bool> RemoveCartItemAsync(Guid accountId, Guid cartItemId, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/carts/account/{accountId}/items/{cartItemId}";
            using var resp = await _httpClient.DeleteAsync(url);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            // 204 No Content = xóa thành công
            return resp.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gọi API backend để xóa nhiều cart item cùng lúc (bulk delete).
        /// DELETE: api/carts/account/{accountId}/items:bulk
        /// </summary>
        public async Task<bool> RemoveCartItemsAsync(Guid accountId, List<Guid> cartItemIds, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/carts/account/{accountId}/items:bulk";
            var body = new RemoveCartItemsRequest { CartItemIds = cartItemIds };

            using var req = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = JsonContent.Create(body)
            };

            using var resp = await _httpClient.SendAsync(req);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            return resp.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gọi API backend để THÊM 1 hoặc NHIỀU option vào CartItem.
        /// POST: api/carts/account/{accountId}/items/{cartItemId}/options
        /// Body: { "optionIds": ["guid1","guid2", ...] }
        /// </summary>
        public async Task<bool> AddOptionsToCartItemAsync(Guid accountId, Guid cartItemId, List<Guid> optionIds, string? jwtToken)
        {
            if (accountId == Guid.Empty || cartItemId == Guid.Empty) return false;
            if (optionIds == null || optionIds.Count == 0) return true; // không có gì để thêm

            SetAuthHeader(jwtToken);

            var url = $"api/carts/account/{accountId}/items/{cartItemId}/options";
            var body = new AddOptionsRequest { OptionIds = optionIds };

            using var resp = await _httpClient.PostAsJsonAsync(url, body);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            // Backend trả Ok({ ok = true }) khi thành công
            if (!resp.IsSuccessStatusCode) return false;

            // nếu muốn đọc { ok = true }:
            try
            {
                var result = await resp.Content.ReadFromJsonAsync<OkFlag>();
                return result?.Ok ?? true; // nếu không có body, coi như OK
            }
            catch
            {
                return true; // nhiều backend trả 200 nhưng không body
            }
        }

        /// <summary>
        /// Gọi API backend để XOÁ 1 option khỏi CartItem.
        /// DELETE: api/carts/account/{accountId}/items/{cartItemId}/options/{optionId}
        /// </summary>
        public async Task<bool> RemoveOptionFromCartItemAsync(Guid accountId, Guid cartItemId, Guid optionId, string? jwtToken)
        {
            if (accountId == Guid.Empty || cartItemId == Guid.Empty || optionId == Guid.Empty) return false;

            SetAuthHeader(jwtToken);

            var url = $"api/carts/account/{accountId}/items/{cartItemId}/options/{optionId}";
            using var resp = await _httpClient.DeleteAsync(url);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            // Backend trả 204 NoContent khi xoá thành công
            return resp.IsSuccessStatusCode;
        }

        // helper nhỏ để đọc { ok = true } từ API
        private sealed class OkFlag { public bool Ok { get; set; } }
    
}
}
