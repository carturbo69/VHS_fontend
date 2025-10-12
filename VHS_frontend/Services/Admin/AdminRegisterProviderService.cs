using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using VHS_frontend.Areas.Admin.Models.RegisterProvider;

namespace VHS_frontend.Services.Admin
{
    public class AdminRegisterProviderService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminRegisterProviderService(HttpClient http) => _http = http;

        // Nếu chưa dùng AuthHeaderHandler:
        public void SetBearerToken(string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<AdminProviderItemDTO>> GetListAsync(string status = "Pending", CancellationToken ct = default)
        {
            var res = await _http.GetAsync($"/api/admin/register-providers?status={status}", ct);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<List<AdminProviderItemDTO>>(_json, ct)) ?? new();
        }

        public async Task<AdminProviderDetailDTO?> GetDetailAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.GetAsync($"/api/admin/register-providers/{id}", ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<AdminProviderDetailDTO>(_json, ct);
        }

        public async Task<bool> ApproveAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.PostAsync($"/api/admin/register-providers/{id}/approve", content: null, ct);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> RejectAsync(Guid id, string? reason = null, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(new { reason }, _json);
            var res = await _http.PostAsync(
                $"/api/admin/register-providers/{id}/reject",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                ct);
            return res.IsSuccessStatusCode;
        }
    }
}
