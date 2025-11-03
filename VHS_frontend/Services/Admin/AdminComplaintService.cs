using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Complaint;

namespace VHS_frontend.Services.Admin
{
    public class AdminComplaintService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        
        public AdminComplaintService(HttpClient http) => _http = http;
        public void SetBearerToken(string token) => _bearer = token;

        private void AttachAuth()
        {
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _bearer);
        }

        private static async Task HandleErrorAsync(HttpResponseMessage res, CancellationToken ct)
        {
            string msg = "Đã có lỗi xảy ra.";
            try
            {
                using var s = await res.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
                if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    msg = m.GetString() ?? msg;
            }
            catch { /* ignore parse error */ }

            res.EnsureSuccessStatusCode();
        }

        public async Task<PaginatedAdminComplaintDTO?> GetAllAsync(AdminComplaintFilterDTO filter, CancellationToken ct = default)
        {
            AttachAuth();
            
            // Build query string
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(filter.Status))
                queryParams.Add($"Status={Uri.EscapeDataString(filter.Status)}");
            if (!string.IsNullOrEmpty(filter.Type))
                queryParams.Add($"Type={Uri.EscapeDataString(filter.Type)}");
            if (!string.IsNullOrEmpty(filter.Search))
                queryParams.Add($"Search={Uri.EscapeDataString(filter.Search)}");
            if (!string.IsNullOrEmpty(filter.SortBy))
                queryParams.Add($"SortBy={Uri.EscapeDataString(filter.SortBy)}");
            if (!string.IsNullOrEmpty(filter.SortOrder))
                queryParams.Add($"SortOrder={Uri.EscapeDataString(filter.SortOrder)}");
            
            queryParams.Add($"Page={filter.Page}");
            queryParams.Add($"PageSize={filter.PageSize}");
            
            var url = $"/api/admin/admincomplaint?{string.Join("&", queryParams)}";
            
            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);

            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<PaginatedAdminComplaintDTO>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ComplaintDetailsDTO?> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/admin/admincomplaint/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                await HandleErrorAsync(res, ct);
            }
            
            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ComplaintDetailsDTO>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<object?> HandleComplaintAsync(Guid id, HandleComplaintDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PutAsync($"/api/admin/admincomplaint/{id}/handle", content, ct);
            await HandleErrorAsync(res, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<object>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ComplaintStatisticsDTO?> GetStatisticsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/admin/admincomplaint/statistics", ct);
            await HandleErrorAsync(res, ct);

            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ComplaintStatisticsDTO>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}








