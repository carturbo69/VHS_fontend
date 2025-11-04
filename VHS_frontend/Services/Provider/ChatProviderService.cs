using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VHS_frontend.Areas.Provider.Models.ChatDTOs;

namespace VHS_frontend.Services.Provider
{
    public class ChatProviderService
    {
        private readonly HttpClient _httpClient;

        public ChatProviderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        static ChatProviderService()
        {
            // Nếu BE trả "Sent"/"Delivered" (PascalCase)
            _jsonOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));

            // Nếu BE trả "sent"/"delivered" (camelCase) thì dùng dòng dưới thay cho dòng trên:
            // _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
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
        /// Lấy danh sách hội thoại (sidebar).
        /// </summary>
        public async Task<List<ConversationListItemVm>> GetConversationsAsync(
            Guid accountId,
            string? jwtToken = null,
            CancellationToken ct = default)
        {
            if (accountId == Guid.Empty) throw new ArgumentException("accountId is required", nameof(accountId));
            SetAuthHeader(jwtToken);

            var url = $"api/Messages/conversations?accountId={Uri.EscapeDataString(accountId.ToString())}";
            using var resp = await _httpClient.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"GET {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            var data = await resp.Content.ReadFromJsonAsync<List<ConversationListItemVm>>(_jsonOptions, ct);
            return data ?? new List<ConversationListItemVm>();
        }

        /// <summary>
        /// Lấy chi tiết một hội thoại (kèm tin nhắn). Hỗ trợ phân trang ngược bằng 'before'.
        /// </summary>
        public async Task<ConversationDto?> GetConversationDetailAsync(
            Guid conversationId,
            Guid accountId,
            int take = 50,
            DateTime? before = null,
            bool markAsRead = true,
            string? jwtToken = null,
            CancellationToken ct = default)
        {
            if (conversationId == Guid.Empty) throw new ArgumentException("conversationId is required", nameof(conversationId));
            if (accountId == Guid.Empty) throw new ArgumentException("accountId is required", nameof(accountId));

            SetAuthHeader(jwtToken);

            var q = new StringBuilder();
            q.Append($"accountId={Uri.EscapeDataString(accountId.ToString())}");
            q.Append($"&take={take}");
            if (before.HasValue)
            {
                // ISO 8601 dạng round-trip để backend parse chuẩn
                q.Append($"&before={Uri.EscapeDataString(before.Value.ToString("O"))}");
            }
            q.Append($"&markAsRead={(markAsRead ? "true" : "false")}");

            var url = $"api/Messages/{conversationId}?{q}";
            using var resp = await _httpClient.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"GET {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            return await resp.Content.ReadFromJsonAsync<ConversationDto>(_jsonOptions, ct);
        }

        /// <summary>
        /// Helper: load thêm tin nhắn cũ hơn (tiện cho UI scroll lên).
        /// </summary>
        public Task<ConversationDto?> LoadMoreMessagesAsync(
            Guid conversationId,
            Guid accountId,
            DateTime before,
            int take = 50,
            string? jwtToken = null,
            CancellationToken ct = default)
            => GetConversationDetailAsync(conversationId, accountId, take, before, markAsRead: false, jwtToken, ct);

        // VHS_frontend/Services/Customer/ChatCustomerService.cs

        public async Task<Guid> FindOrStartConversationByProviderAsync(
            Guid myAccountId,
            Guid providerId,
            string? jwtToken = null,
            CancellationToken ct = default)
        {
            if (myAccountId == Guid.Empty) throw new ArgumentException("myAccountId is required", nameof(myAccountId));
            if (providerId == Guid.Empty) throw new ArgumentException("providerId is required", nameof(providerId));

            SetAuthHeader(jwtToken);

            var url = $"api/Messages/start";
            var payload = new
            {
                MyAccountId = myAccountId,
                ProviderId = providerId
            };

            using var resp = await _httpClient.PostAsJsonAsync(url, payload, _jsonOptions, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"POST {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            var conversationId = await resp.Content.ReadFromJsonAsync<Guid>(_jsonOptions, ct);
            if (conversationId == Guid.Empty)
                throw new InvalidOperationException("Empty ConversationId returned from API.");

            return conversationId;
        }

        public async Task ClearForMeAsync(
    Guid conversationId,
    Guid accountId,
    bool hide = false,
    string? jwtToken = null,
    CancellationToken ct = default)
        {
            if (conversationId == Guid.Empty)
                throw new ArgumentException("conversationId is required", nameof(conversationId));
            if (accountId == Guid.Empty)
                throw new ArgumentException("accountId is required", nameof(accountId));

            SetAuthHeader(jwtToken);

            var url = $"api/Messages/conversations/{conversationId}/me" +
                      $"?accountId={Uri.EscapeDataString(accountId.ToString())}" +
                      $"&hide={(hide ? "true" : "false")}";

            using var resp = await _httpClient.DeleteAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"DELETE {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }
        }


        public async Task SendMessageAsync(
      Guid conversationId,
      Guid accountId,
      string? body,
      IFormFile? image,
      Guid? replyToMessageId = null,
      string? jwtToken = null,
      CancellationToken ct = default)
        {
            if (conversationId == Guid.Empty) throw new ArgumentException("conversationId is required", nameof(conversationId));
            if (accountId == Guid.Empty) throw new ArgumentException("accountId is required", nameof(accountId));

            SetAuthHeader(jwtToken);

            var url = $"api/Messages";

            using var form = new MultipartFormDataContent();

            // ⚠️ Phải trùng tên field với [FromForm] bên backend
            form.Add(new StringContent(conversationId.ToString()), "conversationId");
            form.Add(new StringContent(accountId.ToString()), "accountId");

            // body có thể null; nếu muốn để null đúng nghĩa, có thể bỏ hẳn field này.
            // Ở đây gửi chuỗi rỗng cho tiện UI.
            form.Add(new StringContent(body ?? string.Empty, Encoding.UTF8), "body");

            if (replyToMessageId.HasValue)
            {
                form.Add(new StringContent(replyToMessageId.Value.ToString()), "replyToMessageId");
            }

            if (image != null && image.Length > 0)
            {
                var streamContent = new StreamContent(image.OpenReadStream());
                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(image.ContentType ?? "application/octet-stream");
                form.Add(streamContent, "image", image.FileName);
            }

            using var resp = await _httpClient.PostAsync(url, form, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var respBody = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"POST {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {respBody}");
            }

            // Backend trả { message = "Sent successfully" } – không cần đọc cũng được
            // var result = await resp.Content.ReadFromJsonAsync<{ string message }>(_jsonOptions, ct);
        }

        public async Task<int> GetUnreadTotalAsync(
Guid accountId,
string? jwtToken = null,
CancellationToken ct = default)
        {
            if (accountId == Guid.Empty)
                throw new ArgumentException("accountId is required", nameof(accountId));

            SetAuthHeader(jwtToken);

            var url = $"api/Messages/unread/total?accountId={Uri.EscapeDataString(accountId.ToString())}";
            using var resp = await _httpClient.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"GET {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            // API trả về { total: <int> }
            var payload = await resp.Content.ReadFromJsonAsync<UnreadTotalResponse>(_jsonOptions, ct);
            return payload?.Total ?? 0;
        }

        /// <summary>
        /// Tìm (hoặc tạo) hội thoại 1-1 với một Admin và trả về ConversationId.
        /// </summary>
        public async Task<Guid> FindOrStartConversationWithAdminAsync(
            Guid myAccountId,
            string? jwtToken = null,
            CancellationToken ct = default)
        {
            if (myAccountId == Guid.Empty)
                throw new ArgumentException("myAccountId is required", nameof(myAccountId));

            SetAuthHeader(jwtToken);

            // POST api/Messages/start-with-admin?myAccountId=...
            var url = $"api/Messages/start-with-admin?myAccountId={Uri.EscapeDataString(myAccountId.ToString())}";

            // Có controller nhận param từ query, body không cần. Gửi rỗng.
            using var resp = await _httpClient.PostAsync(url, new StringContent(string.Empty), ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"POST {url} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            var conversationId = await resp.Content.ReadFromJsonAsync<Guid>(_jsonOptions, ct);
            if (conversationId == Guid.Empty)
                throw new InvalidOperationException("Empty ConversationId returned from API.");

            return conversationId;
        }

        private sealed class UnreadTotalResponse
        {
            public int Total { get; set; }
        }
    }
}