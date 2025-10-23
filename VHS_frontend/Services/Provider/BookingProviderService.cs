using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Provider.Models.Booking;

namespace VHS_frontend.Services.Provider
{
    public class BookingProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BookingProviderService(
            HttpClient httpClient, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            var baseUrl = _configuration["Apis:Backend"] ?? "http://localhost:5154";
            _httpClient.BaseAddress = new Uri(baseUrl);
            Console.WriteLine($"[BookingProviderService] BaseAddress set to: {_httpClient.BaseAddress}");
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<BookingListResultDTO?> GetBookingListAsync(BookingFilterDTO filter)
        {
            try
            {
                SetAuthorizationHeader();

                var json = JsonSerializer.Serialize(filter);
                Console.WriteLine($"[BookingService] Request JSON: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "/api/provider/bookings/list";
                Console.WriteLine($"[BookingService] Calling: {_httpClient.BaseAddress}{url}");
                
                var response = await _httpClient.PostAsync(url, content);
                
                Console.WriteLine($"[BookingService] Status Code: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Response: {responseContent}");
                    
                    var result = JsonSerializer.Deserialize<BookingListResultDTO>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    Console.WriteLine($"[BookingService] Deserialized: {result?.Items?.Count ?? 0} items");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error Response: {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BookingService] Exception: {ex.Message}");
                Console.WriteLine($"[BookingService] StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<BookingDetailDTO?> GetBookingDetailAsync(Guid bookingId)
        {
            try
            {
                SetAuthorizationHeader();

                var response = await _httpClient.GetAsync($"/api/provider/bookings/{bookingId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<BookingDetailDTO>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting booking detail: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateBookingStatusAsync(UpdateBookingStatusDTO dto)
        {
            try
            {
                SetAuthorizationHeader();

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("/api/provider/bookings/update-status", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating booking status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AssignStaffAsync(AssignStaffDTO dto)
        {
            try
            {
                SetAuthorizationHeader();

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("/api/provider/bookings/assign-staff", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning staff: {ex.Message}");
                return false;
            }
        }
    }
}

