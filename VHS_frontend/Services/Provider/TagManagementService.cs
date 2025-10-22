using VHS_frontend.Areas.Provider.Models.Service;
using VHS_frontend.Areas.Provider.Models.Tag;
using System.Text.Json;
using System.Net.Http.Json;

namespace VHS_frontend.Services.Provider
{
    public class TagManagementService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public TagManagementService(HttpClient httpClient)
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

        public async Task<List<TagDTO>?> GetAllTagsAsync(string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync("/api/provider/tags", ct);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TagDTO>>>(_json, ct);
            return result?.Data;
        }

        public async Task<List<TagDTO>?> GetTagsByCategoryAsync(string categoryId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"/api/provider/tags/category/{categoryId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TagDTO>>>(_json, ct);
            return result?.Data;
        }

        public async Task<ApiResponse<TagDTO>?> CreateTagAsync(TagCreateDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.PostAsJsonAsync("/api/provider/tags", dto, _json, ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TagDTO>>(_json, ct);
        }

        public async Task<ApiResponse<string>?> DeleteTagAsync(string tagId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.DeleteAsync($"/api/provider/tags/{tagId}", ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_json, ct);
        }
    }
}

