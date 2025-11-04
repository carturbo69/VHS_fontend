using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Provider.Models.Booking;
using VHS_frontend.Areas.Provider.Models.Dashboard;

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
                Console.WriteLine($"[BookingService] GetBookingDetailAsync called with BookingId: {bookingId}");
                
                SetAuthorizationHeader();

                var url = $"/api/provider/bookings/{bookingId}";
                Console.WriteLine($"[BookingService] GET {_httpClient.BaseAddress}{url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[BookingService] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Response length: {responseContent?.Length ?? 0} characters");
                    
                    var result = JsonSerializer.Deserialize<BookingDetailDTO>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    Console.WriteLine($"[BookingService] Deserialized: {(result != null ? "SUCCESS" : "NULL")}");
                    if (result != null)
                    {
                        Console.WriteLine($"[BookingService] Booking: {result.BookingCode}, Status: {result.Status}");
                    }
                    
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error response: {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetBookingDetailAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<bool> UpdateBookingStatusAsync(UpdateBookingStatusDTO dto)
        {
            try
            {
                Console.WriteLine($"[BookingService] UpdateBookingStatusAsync called");
                Console.WriteLine($"[BookingService] DTO: BookingId={dto.BookingId}, NewStatus={dto.NewStatus}");
                
                SetAuthorizationHeader();

                var json = JsonSerializer.Serialize(dto);
                Console.WriteLine($"[BookingService] Request JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "/api/provider/bookings/update-status";
                
                Console.WriteLine($"[BookingService] PUT {_httpClient.BaseAddress}{url}");
                var response = await _httpClient.PutAsync(url, content);
                
                Console.WriteLine($"[BookingService] Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error response: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateBookingStatusAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> AssignStaffAsync(AssignStaffDTO dto)
        {
            try
            {
                Console.WriteLine($"[BookingService] AssignStaffAsync called");
                Console.WriteLine($"[BookingService] DTO: BookingId={dto.BookingId}, StaffId={dto.StaffId}");
                
                SetAuthorizationHeader();

                var json = JsonSerializer.Serialize(dto);
                Console.WriteLine($"[BookingService] Request JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "/api/provider/bookings/assign-staff";
                
                Console.WriteLine($"[BookingService] PUT {_httpClient.BaseAddress}{url}");
                var response = await _httpClient.PutAsync(url, content);
                
                Console.WriteLine($"[BookingService] Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error response: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AssignStaffAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<MonthlyRevenueViewModel?> GetMonthlyRevenueAsync(Guid providerId, int? year = null)
        {
            try
            {
                int targetYear = year ?? DateTime.Now.Year;
                Console.WriteLine($"[BookingService] GetMonthlyRevenueAsync called with ProviderId: {providerId}, Year: {targetYear}");
                
                SetAuthorizationHeader();

                var url = $"/api/provider/bookings/monthly-revenue/{providerId}?year={targetYear}";
                Console.WriteLine($"[BookingService] GET {_httpClient.BaseAddress}{url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[BookingService] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Response: {responseContent}");
                    
                    var result = JsonSerializer.Deserialize<MonthlyRevenueViewModel>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    Console.WriteLine($"[BookingService] Monthly revenue fetched successfully for year {targetYear}");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error response: {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetMonthlyRevenueAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<RevenueReportViewModel?> GetRevenueReportAsync(Guid providerId, RevenueReportFilterViewModel filter)
        {
            try
            {
                Console.WriteLine($"[BookingService] GetRevenueReportAsync called with ProviderId: {providerId}");
                
                SetAuthorizationHeader();

                var requestDto = new
                {
                    ProviderId = providerId,
                    FromDate = filter.FromDate,
                    ToDate = filter.ToDate,
                    Month = filter.Month,
                    Year = filter.Year
                };

                var json = JsonSerializer.Serialize(requestDto);
                Console.WriteLine($"[BookingService] Request JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "/api/provider/bookings/revenue-report";
                
                Console.WriteLine($"[BookingService] POST {_httpClient.BaseAddress}{url}");
                var response = await _httpClient.PostAsync(url, content);
                
                Console.WriteLine($"[BookingService] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Response received");
                    
                    var result = JsonSerializer.Deserialize<RevenueReportViewModel>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    Console.WriteLine($"[BookingService] Revenue report fetched successfully");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error response: {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetRevenueReportAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<BookingStatisticsDTO?> GetProviderStatisticsAsync(Guid providerId)
        {
            try
            {
                Console.WriteLine($"[BookingService] GetProviderStatisticsAsync called with ProviderId: {providerId}");
                
                SetAuthorizationHeader();

                var url = $"/api/provider/bookings/statistics/{providerId}";
                Console.WriteLine($"[BookingService] GET {_httpClient.BaseAddress}{url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[BookingService] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Statistics response received");
                    
                    var result = JsonSerializer.Deserialize<BookingStatisticsDTO>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    Console.WriteLine($"[BookingService] Statistics fetched successfully");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BookingService] Error response: {errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetProviderStatisticsAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}

