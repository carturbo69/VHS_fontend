using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Feedback;

namespace VHS_frontend.Services.Admin
{
    public class AdminFeedbackService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        
        public AdminFeedbackService(HttpClient http) => _http = http;
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

        public async Task<List<FeedbackDTO>> GetAllAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/admin/feedbacks", ct);
            await HandleErrorAsync(res, ct);

            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<List<FeedbackDTO>>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        public async Task<FeedbackDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/admin/feedbacks/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                await HandleErrorAsync(res, ct);
            }
            return await res.Content.ReadFromJsonAsync<FeedbackDTO>(cancellationToken: ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"/api/admin/feedbacks/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }

        public async Task<bool> HideAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PutAsync($"/api/admin/feedbacks/{id}/hide", null, ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }
    }
}
