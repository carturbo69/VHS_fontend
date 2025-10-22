using VHS_frontend.Areas.Provider.Models.Service;
using VHS_frontend.Areas.Provider.Models.Option;
using System.Text.Json;
using System.Net.Http.Json;

namespace VHS_frontend.Services.Provider
{
    public class OptionManagementService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public OptionManagementService(HttpClient httpClient)
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

        public async Task<List<OptionDTO>?> GetAllOptionsAsync(string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync("/api/provider/options", ct);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<OptionDTO>>>(_json, ct);
            return result?.Data;
        }

        public async Task<ApiResponse<OptionDTO>?> CreateOptionAsync(OptionCreateDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("/api/provider/options", dto, _json, ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<OptionDTO>>(_json, ct);
        }

        public async Task<ApiResponse<string>?> DeleteOptionAsync(string optionId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.DeleteAsync($"/api/provider/options/{optionId}", ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_json, ct);
        }
    }
}

