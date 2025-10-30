using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            throw new HttpRequestException(msg);
        }

        private static readonly JsonSerializerOptions _jsonOptions =
            new() { PropertyNameCaseInsensitive = true };

        private static StringContent EmptyJson()
            => new StringContent(string.Empty, Encoding.UTF8, "application/json");

        private const string BasePath = "/api/reviews";

        // ----- LIST ALL (admin-all) -----
        public async Task<List<FeedbackDTO>> GetAllAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"{BasePath}/admin-all", ct);
            await HandleErrorAsync(res, ct);

            var raw = await res.Content.ReadAsStringAsync(ct);
            try
            {
                var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<FeedbackDTO>>>(raw, _jsonOptions);
                if (envelope?.Success == true && envelope.Data != null)
                    return envelope.Data;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Không parse được JSON từ API /api/reviews/admin-all. Nội dung: " + raw, ex);
            }

            return new List<FeedbackDTO>();
        }

        // ----- GET BY ID (không có endpoint riêng -> lọc từ admin-all) -----
        public async Task<FeedbackDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct);
            return all.FirstOrDefault(x => x.Id == id);
        }

        // ----- SOFT DELETE (xóa mềm) -----
        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"{BasePath}/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }

        // ----- HIDE (ẩn = IsDeleted = true) -----
        public async Task<bool> HideAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            // Controller dùng POST /api/reviews/{id}/hide
            var res = await _http.PostAsync($"{BasePath}/{id}/hide", EmptyJson(), ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }

        // ----- SHOW (hiện = IsDeleted = false) -----
        public async Task<bool> ShowAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            // Controller dùng POST /api/reviews/{id}/show
            var res = await _http.PostAsync($"{BasePath}/{id}/show", EmptyJson(), ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }

        // ----- Envelope chung { success, data, message } -----
        private sealed class ApiEnvelope<T>
        {
            [JsonPropertyName("success")] public bool Success { get; set; }
            [JsonPropertyName("data")] public T? Data { get; set; }
            [JsonPropertyName("message")] public string? Message { get; set; }
        }
    }
}
