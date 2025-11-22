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
            // Thêm các trường cho pending update
            public string? PendingTitle { get; set; }
            public decimal? PendingPrice { get; set; }
            public string? PendingUnitType { get; set; }
            public int? PendingBaseUnit { get; set; }
            public string? PendingImages { get; set; }
            public string? PendingTagIds { get; set; } // JSON array of Guid
            public DateTime? PendingUpdatedAt { get; set; }
            // Dữ liệu hiện tại (để so sánh)
            public string? CurrentTitle { get; set; }
            public decimal? CurrentPrice { get; set; }
            public string? CurrentUnitType { get; set; }
            public int? CurrentBaseUnit { get; set; }
            public string? CurrentImages { get; set; }
            public List<Guid>? CurrentTagIds { get; set; } // List of current Tag IDs
            // Tags objects (from backend)
            public List<TagInfo>? Tags { get; set; } // Tags for Pending services
            public List<TagInfo>? CurrentTags { get; set; } // Current Tags for PendingUpdate services
            public List<TagInfo>? PendingTags { get; set; } // Pending Tags for PendingUpdate services
        }

        public class TagInfo
        {
            public Guid TagId { get; set; }
            public string Name { get; set; } = string.Empty;
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
            // Dữ liệu hiển thị (ưu tiên pending nếu có)
            public List<TagDTO> Tags { get; set; } = new();
            public List<OptionDTO> Options { get; set; } = new();
            // Current data (để so sánh)
            public string? CurrentTitle { get; set; }
            public string? CurrentDescription { get; set; }
            public decimal? CurrentPrice { get; set; }
            public string? CurrentUnitType { get; set; }
            public int? CurrentBaseUnit { get; set; }
            public string? CurrentImages { get; set; }
            public List<TagInfo>? CurrentTags { get; set; }
            public List<OptionDTO>? CurrentOptions { get; set; }
            // Pending data
            public string? PendingTitle { get; set; }
            public string? PendingDescription { get; set; }
            public decimal? PendingPrice { get; set; }
            public string? PendingUnitType { get; set; }
            public int? PendingBaseUnit { get; set; }
            public string? PendingImages { get; set; }
            public List<TagInfo>? PendingTags { get; set; }
            public List<OptionDTO>? PendingOptions { get; set; }
            public DateTime? PendingUpdatedAt { get; set; }
        }

        public class TagDTO { public Guid TagId { get; set; } public string Name { get; set; } = string.Empty; }
        public class OptionDTO { public Guid OptionId { get; set; } public string OptionName { get; set; } = string.Empty; public Guid? TagId { get; set; } public string Type { get; set; } = string.Empty; public Guid? Family { get; set; } public string? Value { get; set; } }

        public async Task<List<PendingServiceItem>?> GetPendingAsync(string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var allPending = new List<PendingServiceItem>();
            
            // Lấy dịch vụ mới tạo (Status = "Pending")
            var respNew = await _httpClient.GetAsync("/api/admin/services/pending", ct);
            if (respNew.IsSuccessStatusCode)
            {
                var resultNew = await respNew.Content.ReadFromJsonAsync<ApiResponse<List<PendingServiceItem>>>(_json, ct);
                if (resultNew?.Data != null)
                {
                    allPending.AddRange(resultNew.Data);
                }
            }
            
            // Lấy dịch vụ chỉnh sửa chờ duyệt (Status = "PendingUpdate")
            var respUpdate = await _httpClient.GetAsync("/api/admin/services/pending-updates", ct);
            if (respUpdate.IsSuccessStatusCode)
            {
                var resultUpdate = await respUpdate.Content.ReadFromJsonAsync<ApiResponse<List<PendingServiceItem>>>(_json, ct);
                if (resultUpdate?.Data != null)
                {
                    allPending.AddRange(resultUpdate.Data);
                }
            }
            
            return allPending.Any() ? allPending : null;
        }

        public async Task<bool> ApproveAsync(Guid serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var resp = await _httpClient.PutAsync($"/api/admin/services/{serviceId}/approve", null, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> ApproveUpdateAsync(Guid serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var resp = await _httpClient.PutAsync($"/api/admin/services/{serviceId}/approve-update", null, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<PendingServiceItem?> GetServiceStatusAsync(Guid serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            // Lấy từ pending list để biết status
            var allPending = await GetPendingAsync(token, ct);
            return allPending?.FirstOrDefault(s => s.ServiceId == serviceId);
        }

        public async Task<bool> RejectAsync(Guid serviceId, string? reason = null, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var content = JsonContent.Create(new { reason = reason ?? string.Empty }, options: _json);
            var resp = await _httpClient.PutAsync($"/api/admin/services/{serviceId}/reject", content, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> RejectUpdateAsync(Guid serviceId, string? reason = null, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var content = JsonContent.Create(new { reason = reason ?? string.Empty }, options: _json);
            var resp = await _httpClient.PutAsync($"/api/admin/services/{serviceId}/reject-update", content, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<ServiceDetail?> GetDetailAsync(Guid serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            // Dùng API Admin để lấy cả current và pending data
            var resp = await _httpClient.GetAsync($"/api/admin/services/{serviceId}/detail", ct);
            if (!resp.IsSuccessStatusCode) return null;
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<ServiceDetail>>(_json, ct);
            return result?.Data;
        }
    }
}


