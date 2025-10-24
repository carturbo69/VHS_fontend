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
                var profileResponse = await response.Content.ReadFromJsonAsync<ProfileResponseDTO>(options);
                
                if (profileResponse?.Success == true && profileResponse.Data != null)
                {
                    var json = JsonSerializer.Serialize(profileResponse.Data);
                    return JsonSerializer.Deserialize<ViewProfileDTO>(json, options);
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
        public async Task<ProfileResponseDTO> RequestPasswordChangeOTPAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.PostAsync("api/profile/request-password-change-otp", null);
                
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
        public async Task<ProfileResponseDTO> RequestEmailChangeOTPAsync(string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            try
            {
                var response = await _httpClient.PostAsync("api/profile/request-email-change-otp", null);
                
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
