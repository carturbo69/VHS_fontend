using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Areas.Customer.Models.Profile;

namespace VHS_frontend.Services.Customer
{
    /// <summary>
    /// Service để gọi API UserAddress từ Backend
    /// </summary>
    public class UserAddressService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UserAddressService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Lấy tất cả địa chỉ của user từ Backend API
        /// </summary>
        public async Task<List<UserAddressDto>> GetUserAddressesAsync(string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/UserAddress");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new List<UserAddressDto>();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(jsonString);
                
                // Parse response: { Success = true, Data = [ {...}, {...} ] }
                if (result.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    var addresses = new List<UserAddressDto>();
                    foreach (var item in data.EnumerateArray())
                    {
                        var address = new UserAddressDto
                        {
                            AddressId = item.TryGetProperty("addressId", out var addrId) ? Guid.Parse(addrId.GetString() ?? "00000000-0000-0000-0000-000000000000") : Guid.Empty,
                            StreetAddress = item.TryGetProperty("streetAddress", out var street) ? street.GetString() ?? "" : "",
                            WardName = item.TryGetProperty("wardName", out var ward) ? ward.GetString() ?? "" : "",
                            DistrictName = item.TryGetProperty("districtName", out var district) ? district.GetString() ?? "" : "",
                            ProvinceName = item.TryGetProperty("provinceName", out var province) ? province.GetString() ?? "" : "",
                            // ✅ Lấy tọa độ từ API
                            Latitude = item.TryGetProperty("latitude", out var lat) && lat.ValueKind != JsonValueKind.Null ? lat.GetDouble() : null,
                            Longitude = item.TryGetProperty("longitude", out var lng) && lng.ValueKind != JsonValueKind.Null ? lng.GetDouble() : null
                        };
                        addresses.Add(address);
                    }
                    return addresses;
                }
                
                return new List<UserAddressDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user addresses: {ex.Message}");
                return new List<UserAddressDto>();
            }
        }

        /// <summary>
        /// Lấy địa chỉ theo ID
        /// </summary>
        public async Task<UserAddressDto?> GetAddressByIdAsync(Guid addressId, string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/UserAddress/{addressId}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(jsonString);
                
                if (result.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    return new UserAddressDto
                    {
                        AddressId = data.TryGetProperty("addressId", out var addrId) ? Guid.Parse(addrId.GetString() ?? "00000000-0000-0000-0000-000000000000") : Guid.Empty,
                        StreetAddress = data.TryGetProperty("streetAddress", out var street) ? street.GetString() ?? "" : "",
                        WardName = data.TryGetProperty("wardName", out var ward) ? ward.GetString() ?? "" : "",
                        DistrictName = data.TryGetProperty("districtName", out var district) ? district.GetString() ?? "" : "",
                        ProvinceName = data.TryGetProperty("provinceName", out var province) ? province.GetString() ?? "" : "",
                        // ✅ Lấy tọa độ từ API
                        Latitude = data.TryGetProperty("latitude", out var lat) && lat.ValueKind != JsonValueKind.Null ? lat.GetDouble() : null,
                        Longitude = data.TryGetProperty("longitude", out var lng) && lng.ValueKind != JsonValueKind.Null ? lng.GetDouble() : null
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching address by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tạo địa chỉ mới
        /// </summary>
        public async Task<ProfileResponseDTO> CreateAddressAsync(string provinceName, string districtName, 
            string wardName, string streetAddress, double? latitude, double? longitude, string jwt)
        {
            try
            {
                var dto = new
                {
                    ProvinceName = provinceName,
                    DistrictName = districtName,
                    WardName = wardName,
                    StreetAddress = streetAddress,
                    Latitude = latitude,
                    Longitude = longitude
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/UserAddress")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ProfileResponseDTO { Success = false, Message = jsonString };
                }

                var result = JsonSerializer.Deserialize<JsonElement>(jsonString);

                return new ProfileResponseDTO
                {
                    Success = result.TryGetProperty("success", out var success) && success.GetBoolean(),
                    Message = result.TryGetProperty("message", out var message) ? message.GetString() ?? "" : ""
                };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Xóa địa chỉ
        /// </summary>
        public async Task<ProfileResponseDTO> DeleteAddressAsync(Guid addressId, string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/UserAddress/{addressId}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ProfileResponseDTO { Success = false, Message = jsonString };
                }

                var result = JsonSerializer.Deserialize<JsonElement>(jsonString);

                return new ProfileResponseDTO
                {
                    Success = result.TryGetProperty("success", out var success) && success.GetBoolean(),
                    Message = result.TryGetProperty("message", out var message) ? message.GetString() ?? "" : ""
                };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }
    }
}

