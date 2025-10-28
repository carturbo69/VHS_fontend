using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using VHS_frontend.Areas.Provider.Models.Feedback;

namespace VHS_frontend.Services.Provider
{
    public class ProviderFeedbackService
    {
        private readonly HttpClient _httpClient;

        public ProviderFeedbackService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private void SetAuthHeader(string? jwtToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }
        }

        /// <summary>
        /// Gọi API backend để lấy feedback theo service cho provider (qua accountId).
        /// Trả thẳng ProviderFeedbackViewModel (không cần map).
        /// </summary>
        public async Task<ProviderFeedbackViewModel> GetFeedbacksAsync(
            Guid accountId,
            string? jwtToken,
            CancellationToken ct = default)
        {
            SetAuthHeader(jwtToken);

            using var res = await _httpClient.GetAsync($"/api/reviews/provider/{accountId}/feedbacks", ct);

            // Ném lỗi rõ ràng nếu không 2xx
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                var msg = $"Lỗi gọi API ({(int)res.StatusCode} {res.ReasonPhrase}). Body: {body}";
                throw new HttpRequestException(msg);
            }

            await using var stream = await res.Content.ReadAsStreamAsync(ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var envelope = await JsonSerializer.DeserializeAsync<ApiEnvelope<ProviderFeedbackViewModel>>(stream, options, ct);

            // Nếu success=false hoặc data=null, trả rỗng cho an toàn
            return (envelope?.Success == true && envelope.Data != null)
                ? envelope.Data
                : new ProviderFeedbackViewModel();
        }

        /// <summary>
        /// Provider gửi phản hồi cho một review.
        /// Gọi: POST /api/reviews/provider/{accountId}/reply  (body: { reviewId, content })
        /// Trả về true nếu success=true.
        /// </summary>
        public async Task<bool> SendReplyAsync(
            Guid accountId,
            ProviderReplyRequestDto dto,
            string? jwtToken,
            CancellationToken ct = default)
        {
            if (dto == null || dto.ReviewId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Dữ liệu phản hồi không hợp lệ.");

            SetAuthHeader(jwtToken);

            using var res = await _httpClient.PostAsJsonAsync(
                $"/api/reviews/provider/{accountId}/reply",
                dto,
                ct);

            // Nếu không 2xx: đọc body để log lỗi rõ ràng
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                var msg = $"Lỗi gọi API ({(int)res.StatusCode} {res.ReasonPhrase}). Body: {body}";
                throw new HttpRequestException(msg);
            }

            await using var stream = await res.Content.ReadAsStreamAsync(ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var envelope = await JsonSerializer.DeserializeAsync<ApiEnvelope<object>>(stream, options, ct);

            return envelope?.Success == true;
        }

        // ====== Envelope theo API { success, data, message } ======
        private sealed class ApiEnvelope<T>
        {
            [JsonPropertyName("success")] public bool Success { get; set; }
            [JsonPropertyName("data")] public T? Data { get; set; }
            [JsonPropertyName("message")] public string? Message { get; set; }
        }
    }
}
