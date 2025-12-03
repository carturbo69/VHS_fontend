using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Customer.Models.Profile;

namespace VHS_frontend.Services.Customer
{
    /// <summary>
    /// Service để giao tiếp với Profile API Backend
    /// </summary>
    public class ProfileServiceCustomer
    {
        private readonly HttpClient _httpClient;

        public ProfileServiceCustomer(HttpClient httpClient)
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
        /// Lấy thông tin profile của user hiện tại
        /// </summary>
        public async Task<ViewProfileDTO?> GetProfileAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.GetAsync("api/profile");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                // Log JSON response for debugging
                var jsonString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] JSON Response: {jsonString}");
                
                // Try parse as direct ViewProfileDTO first
                try
                {
                    var profile = JsonSerializer.Deserialize<ViewProfileDTO>(jsonString, options);
                    System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Parsed as direct ViewProfileDTO: {profile?.AccountName}");
                    return profile;
                }
                catch
                {
                    // Try parse as ProfileResponseDTO wrapper
                    var profileResponse = JsonSerializer.Deserialize<ProfileResponseDTO>(jsonString, options);
                    System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Success: {profileResponse?.Success}, Data: {profileResponse?.Data}");
                    
                    if (profileResponse?.Success == true && profileResponse.Data != null)
                    {
                        var json = JsonSerializer.Serialize(profileResponse.Data);
                        return JsonSerializer.Deserialize<ViewProfileDTO>(json, options);
                    }
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Lỗi khi gọi API: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật thông tin profile
        /// </summary>
        public async Task<ProfileResponseDTO> UpdateProfileAsync(EditProfileDTO editDto, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/profile", editDto);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<ProfileResponseDTO>(options) 
                       ?? new ProfileResponseDTO { Success = false, Message = "Không thể parse response" };
            }
            catch (HttpRequestException ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi khi gọi API: {ex.Message}" };
            }
        }

        /// <summary>
        /// Request OTP để đổi mật khẩu
        /// </summary>
        public async Task<OTPResponseDTO> RequestPasswordChangeOTPAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                System.Diagnostics.Debug.WriteLine("[ProfileServiceCustomer] Calling request-password-change-otp...");
                var response = await _httpClient.PostAsync("api/profile/request-password-change-otp", null);
                
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Response status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                var jsonString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Response body: {jsonString}");

                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse error message from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<OTPResponseDTO>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return errorResponse ?? new OTPResponseDTO { Success = false, Message = $"Lỗi từ server: {response.StatusCode}" };
                    }
                    catch
                    {
                        return new OTPResponseDTO { Success = false, Message = $"Lỗi từ server: {response.StatusCode} - {jsonString}" };
                    }
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OTPResponseDTO>(jsonString, options);
                return result ?? new OTPResponseDTO { Success = false, Message = "Không thể parse response" };
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] HttpRequestException: {ex.Message}");
                return new OTPResponseDTO { Success = false, Message = $"Lỗi khi gọi API: {ex.Message}" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Exception: {ex.Message}");
                return new OTPResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Đổi mật khẩu với OTP
        /// </summary>
        public async Task<ProfileResponseDTO> ChangePasswordAsync(ChangePasswordDTO changeDto, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/profile/change-password", changeDto);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<ProfileResponseDTO>(options) 
                       ?? new ProfileResponseDTO { Success = false, Message = "Không thể parse response" };
            }
            catch (HttpRequestException ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi khi gọi API: {ex.Message}" };
            }
        }

        /// <summary>
        /// Request OTP để đổi email
        /// </summary>
        public async Task<OTPResponseDTO> RequestEmailChangeOTPAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                System.Diagnostics.Debug.WriteLine("[ProfileServiceCustomer] Calling request-email-change-otp...");
                var response = await _httpClient.PostAsync("api/profile/request-email-change-otp", null);
                
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Response status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                var jsonString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Response body: {jsonString}");

                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse error message from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<OTPResponseDTO>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return errorResponse ?? new OTPResponseDTO { Success = false, Message = $"Lỗi từ server: {response.StatusCode}" };
                    }
                    catch
                    {
                        return new OTPResponseDTO { Success = false, Message = $"Lỗi từ server: {response.StatusCode} - {jsonString}" };
                    }
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OTPResponseDTO>(jsonString, options);
                return result ?? new OTPResponseDTO { Success = false, Message = "Không thể parse response" };
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] HttpRequestException: {ex.Message}");
                return new OTPResponseDTO { Success = false, Message = $"Lỗi khi gọi API: {ex.Message}" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileServiceCustomer] Exception: {ex.Message}");
                return new OTPResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Đổi email với OTP
        /// </summary>
        public async Task<ProfileResponseDTO> ChangeEmailAsync(ChangeEmailDTO changeDto, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/profile/change-email", changeDto);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                // Read response content before checking status
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse error message from response
                    string errorMessage = "Có lỗi xảy ra khi đổi email";
                    
                    try
                    {
                        var errorResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                        
                        // Try multiple common error message fields
                        if (errorResult.TryGetProperty("message", out var messageProp))
                            errorMessage = messageProp.GetString() ?? errorMessage;
                        else if (errorResult.TryGetProperty("Message", out var messageProp2))
                            errorMessage = messageProp2.GetString() ?? errorMessage;
                        else if (errorResult.TryGetProperty("error", out var errorProp))
                            errorMessage = errorProp.GetString() ?? errorMessage;
                        else if (errorResult.TryGetProperty("Error", out var errorProp2))
                            errorMessage = errorProp2.GetString() ?? errorMessage;
                        else if (errorResult.TryGetProperty("detail", out var detailProp))
                            errorMessage = detailProp.GetString() ?? errorMessage;
                        else if (errorResult.TryGetProperty("Detail", out var detailProp2))
                            errorMessage = detailProp2.GetString() ?? errorMessage;
                        else if (errorResult.TryGetProperty("title", out var titleProp))
                            errorMessage = titleProp.GetString() ?? errorMessage;
                        
                        // Check for validation errors
                        if (errorResult.TryGetProperty("errors", out var errorsProp))
                        {
                            var errors = new List<string>();
                            foreach (var error in errorsProp.EnumerateObject())
                            {
                                if (error.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var err in error.Value.EnumerateArray())
                                    {
                                        if (err.ValueKind == System.Text.Json.JsonValueKind.String)
                                            errors.Add(err.GetString() ?? "");
                                    }
                                }
                                else if (error.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    errors.Add(error.Value.GetString() ?? "");
                                }
                            }
                            if (errors.Count > 0)
                                errorMessage = string.Join(", ", errors);
                        }
                    }
                    catch
                    {
                        // If can't parse JSON, use response content if it's short enough
                        if (responseContent.Length < 500)
                            errorMessage = responseContent;
                    }
                    
                    return new ProfileResponseDTO { Success = false, Message = errorMessage };
                }

                // Parse success response
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return System.Text.Json.JsonSerializer.Deserialize<ProfileResponseDTO>(responseContent, options) 
                       ?? new ProfileResponseDTO { Success = false, Message = "Không thể parse response" };
            }
            catch (UnauthorizedAccessException)
            {
                return new ProfileResponseDTO { Success = false, Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." };
            }
            catch (HttpRequestException ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Có lỗi xảy ra: {ex.Message}" };
            }
        }

        /// <summary>
        /// Kiểm tra profile completeness
        /// </summary>
        public async Task<dynamic> CheckProfileCompletenessAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.GetAsync("api/profile/completeness");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<dynamic>(options);
            }
            catch (HttpRequestException ex)
            {
                return new { success = false, message = $"Lỗi khi gọi API: {ex.Message}" };
            }
        }

        /// <summary>
        /// Upload ảnh profile
        /// </summary>
        public async Task<dynamic> UploadProfileImageAsync(IFormFile image, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = image.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                content.Add(streamContent, "Image", image.FileName);

                var response = await _httpClient.PostAsync("api/profile/upload-image", content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<dynamic>(options);
            }
            catch (HttpRequestException ex)
            {
                return new { success = false, message = $"Lỗi khi gọi API: {ex.Message}" };
            }
        }

        /// <summary>
        /// Xóa ảnh profile
        /// </summary>
        public async Task<dynamic> DeleteProfileImageAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.DeleteAsync("api/profile/delete-image");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await response.Content.ReadFromJsonAsync<dynamic>(options);
            }
            catch (HttpRequestException ex)
            {
                return new { success = false, message = $"Lỗi khi gọi API: {ex.Message}" };
            }
        }
    }
}
