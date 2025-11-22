using System.Net.Http.Json;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Option;

namespace VHS_frontend.Services.Admin
{
    public class OptionAdminService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public OptionAdminService(HttpClient http) => _http = http;

        public async Task<List<OptionDTO>?> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync("/api/provider/options", ct);
                if (!response.IsSuccessStatusCode) return new List<OptionDTO>();
                
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<OptionDTO>>>(_json, ct);
                return result?.Data ?? new List<OptionDTO>();
            }
            catch
            {
                return new List<OptionDTO>();
            }
        }

        public async Task<List<OptionDTO>?> GetByTagAsync(Guid tagId, CancellationToken ct = default)
        {
            try
            {
                // Lấy tất cả options và filter theo TagId ở frontend
                var allOptions = await GetAllAsync(ct);
                return allOptions?.Where(o => o.TagId == tagId).ToList() ?? new List<OptionDTO>();
            }
            catch
            {
                return new List<OptionDTO>();
            }
        }

        public async Task<OptionDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync($"/api/provider/options/{id}", ct);
                if (!response.IsSuccessStatusCode) return null;
                
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<OptionDTO>>(_json, ct);
                return result?.Data;
            }
            catch
            {
                return null;
            }
        }

        public Task<HttpResponseMessage> CreateAsync(OptionCreateDTO dto, CancellationToken ct = default)
            => _http.PostAsJsonAsync("/api/provider/options", dto, _json, ct);

        public Task<HttpResponseMessage> UpdateAsync(Guid id, OptionUpdateDTO dto, CancellationToken ct = default)
            => _http.PutAsJsonAsync($"/api/provider/options/{id}", dto, _json, ct);

        public Task<HttpResponseMessage> DeleteAsync(Guid id, CancellationToken ct = default)
            => _http.DeleteAsync($"/api/provider/options/{id}", ct);
    }

    // Helper class for API response
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}

