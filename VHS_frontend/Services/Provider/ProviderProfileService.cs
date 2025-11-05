using VHS_frontend.Areas.Provider.Models.Profile;
using VHS_frontend.Areas.Admin.Models.RegisterProvider;
using System.Text.Json;

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
            return await _httpClient.PutAsJsonAsync($"/api/provider/profile/{accountId}", updateModel, _json, ct);
        }

        public async Task<string?> GetProviderIdByAccountAsync(string accountId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.GetFromJsonAsync<string>($"/api/provider/get-id-by-account/{accountId}", _json, ct);
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
