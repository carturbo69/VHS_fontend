using VHS_frontend.Areas.Provider.Models.Profile;
using VHS_frontend.Areas.Admin.Models.RegisterProvider;
using System.Text.Json;
using System.Text;

namespace VHS_frontend.Services.Provider
{
    public class ProviderProfileService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public ProviderProfileService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private void SetAuthHeader(string? token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<ProviderProfileDTO?> GetProfileAsync(string accountId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.GetFromJsonAsync<ProviderProfileDTO>(
                $"/api/provider/profile/{accountId}", 
                _json, ct);
        }

        public async Task<HttpResponseMessage> UpdateProfileAsync(string accountId, ProviderProfileUpdateDTO updateModel, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            
            // ✅ Tạo MultipartFormDataContent để gửi file và form data
            var formData = new MultipartFormDataContent();
            
            // Thêm các field text
            formData.Add(new StringContent(updateModel.ProviderName), "ProviderName");
            formData.Add(new StringContent(updateModel.PhoneNumber), "PhoneNumber");
            if (!string.IsNullOrEmpty(updateModel.Description))
            {
                formData.Add(new StringContent(updateModel.Description), "Description");
            }
            
            // ✅ Thêm file ảnh nếu có
            if (updateModel.ImageFile != null && updateModel.ImageFile.Length > 0)
            {
                var imageContent = new StreamContent(updateModel.ImageFile.OpenReadStream());
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(updateModel.ImageFile.ContentType);
                formData.Add(imageContent, "ProfileImage", updateModel.ImageFile.FileName);
            }
            else if (!string.IsNullOrEmpty(updateModel.Images))
            {
                // Nếu không có file mới, gửi URL ảnh cũ
                formData.Add(new StringContent(updateModel.Images), "Images");
            }
            
            return await _httpClient.PutAsync($"/api/provider/profile/{accountId}", formData, ct);
        }

        public async Task<string?> GetProviderIdByAccountAsync(string accountId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.GetFromJsonAsync<string>($"/api/provider/get-id-by-account/{accountId}", _json, ct);
        }

        public async Task<HttpResponseMessage> SendOTPForChangePasswordAsync(string accountId, SendOTPRequestDTO request, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.PostAsJsonAsync($"/api/provider/send-otp-change-password/{accountId}", request, _json, ct);
        }

        public async Task<HttpResponseMessage> ValidatePasswordAndSendOTPAsync(string accountId, ValidatePasswordAndSendOTPDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.PostAsJsonAsync($"/api/provider/validate-password-and-send-otp/{accountId}", dto, _json, ct);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(string accountId, ChangePasswordDTO changePasswordModel, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.PostAsJsonAsync($"/api/provider/change-password/{accountId}", changePasswordModel, _json, ct);
        }

        public async Task<UploadImageResultDTO?> UploadAvatarAsync(IFormFile imageFile, string? token = null, CancellationToken ct = default)
        {
            try
            {
                SetAuthHeader(token);
                
                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "IMAGE", imageFile.FileName);

                var response = await _httpClient.PostAsync("/api/register-provider/media/avatar", content, ct);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync(ct);
                    return JsonSerializer.Deserialize<UploadImageResultDTO>(jsonString, _json);
                }
                else
                {
                    var errorString = await response.Content.ReadAsStringAsync(ct);
                    return new UploadImageResultDTO 
                    { 
                        Success = false, 
                        Message = $"Upload failed: {errorString}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new UploadImageResultDTO 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }
    }
}
