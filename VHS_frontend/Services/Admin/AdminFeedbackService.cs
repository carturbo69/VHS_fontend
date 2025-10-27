using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using VHS_frontend.Areas.Admin.Models.Feedback;
// ...

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
            if (res.IsSuccessStatusCode) return;

            string msg = $"HTTP {(int)res.StatusCode} {res.ReasonPhrase}";
            try
            {
                using var s = await res.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
                if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    msg = m.GetString() ?? msg;
            }
            catch { /* ignore parse error */ }

            // NÉN vứt EnsureSuccessStatusCode(); ném lỗi có message custom
            throw new HttpRequestException(msg);
        }


        public async Task<List<FeedbackDTO>> GetAllAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/reviews/admin-all", ct);
            await HandleErrorAsync(res, ct);

            var raw = await res.Content.ReadAsStringAsync(ct); // DEBUG: xem raw
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<FeedbackDTO>>>(raw, options);
                if (envelope?.Success == true && envelope.Data != null)
                    return envelope.Data;
            }
            catch (Exception ex)
            {
                // có thể log ex + raw để thấy server trả cái gì
                throw new InvalidOperationException("Không parse được JSON từ API /api/reviews/admin-all. Nội dung: " + raw, ex);
            }

            return new List<FeedbackDTO>();
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

        // Envelope để deserialize { success, data, message }
        private sealed class ApiEnvelope<T>
        {
            [JsonPropertyName("success")] public bool Success { get; set; }
            [JsonPropertyName("data")] public T? Data { get; set; }
            [JsonPropertyName("message")] public string? Message { get; set; }
        }
    }
}
