using VHS_frontend.Areas.Provider.Models.Profile;
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
    }
}
