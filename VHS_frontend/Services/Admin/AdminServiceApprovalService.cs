using System.Text.Json;

namespace VHS_frontend.Services.Admin
{
    public class AdminServiceApprovalService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public AdminServiceApprovalService(HttpClient httpClient)
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

        public class PendingServiceItem
        {
            public Guid ServiceId { get; set; }
            public Guid ProviderId { get; set; }
            public Guid CategoryId { get; set; }
            public string Title { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string UnitType { get; set; } = string.Empty;
            public int? BaseUnit { get; set; }
            public string? Images { get; set; }
            public string? Status { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
        }

        public class ServiceDetail
        {
            public Guid ServiceId { get; set; }
            public Guid ProviderId { get; set; }
            public Guid CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string UnitType { get; set; } = string.Empty;
            public int BaseUnit { get; set; }
            public string? Images { get; set; }
            public string? Status { get; set; }
            public List<TagDTO> Tags { get; set; } = new();
            public List<OptionDTO> Options { get; set; } = new();
        }

        public class TagDTO { public Guid TagId { get; set; } public string Name { get; set; } = string.Empty; }
        public class OptionDTO { public Guid OptionId { get; set; } public string OptionName { get; set; } = string.Empty; public string? Description { get; set; } public decimal Price { get; set; } public string UnitType { get; set; } = string.Empty; }

        public async Task<List<PendingServiceItem>?> GetPendingAsync(string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var resp = await _httpClient.GetAsync("/api/admin/services/pending", ct);
            if (!resp.IsSuccessStatusCode) return null;
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<List<PendingServiceItem>>>(_json, ct);
            return result?.Data;
        }

        public async Task<bool> ApproveAsync(Guid serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var resp = await _httpClient.PutAsync($"/api/admin/services/{serviceId}/approve", null, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> RejectAsync(Guid serviceId, string? reason = null, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var content = JsonContent.Create(new { reason = reason ?? string.Empty }, options: _json);
            var resp = await _httpClient.PutAsync($"/api/admin/services/{serviceId}/reject", content, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<ServiceDetail?> GetDetailAsync(Guid serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            // dùng API ServiceProvider để lấy đủ Tags/Options
            var resp = await _httpClient.GetAsync($"/api/ServiceProvider/{serviceId}", ct);
            if (!resp.IsSuccessStatusCode) return null;
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<ServiceDetail>>(_json, ct);
            return result?.Data;
        }
    }
}


