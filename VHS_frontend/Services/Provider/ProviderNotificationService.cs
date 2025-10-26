// Services/Provider/ProviderNotificationService.cs
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Provider.Models.Notification;

namespace VHS_frontend.Services.Provider
{
    public class ApiBadRequestException : Exception
    {
        public ApiBadRequestException(string message) : base(message) { }
    }

    public class ProviderNotificationService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        public ProviderNotificationService(HttpClient http) => _http = http;
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

            if (res.StatusCode == HttpStatusCode.BadRequest)
                throw new ApiBadRequestException(msg);

            res.EnsureSuccessStatusCode(); // sẽ ném HttpRequestException cho các status khác
        }

        // GET: Lấy số lượng thông báo chưa đọc
        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                AttachAuth();
                var response = await _http.GetFromJsonAsync<object>("/api/notification/unread");
                // Parse response để lấy unreadCount
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response);
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("unreadCount", out var countElement))
                    {
                        return countElement.GetInt32();
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return 0;
            }
        }

        // GET: Lấy danh sách thông báo chưa đọc
        public async Task<List<ProviderNotificationDTO>> GetUnreadNotificationsAsync()
        {
            try
            {
                AttachAuth();
                var response = await _http.GetFromJsonAsync<object>("/api/notification/unread");
                // Parse response để lấy danh sách notifications
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response);
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        return JsonSerializer.Deserialize<List<ProviderNotificationDTO>>(
                            dataElement.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ProviderNotificationDTO>();
                    }
                }
                return new List<ProviderNotificationDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread notifications: {ex.Message}");
                return new List<ProviderNotificationDTO>();
            }
        }

        // GET: Lấy tất cả thông báo của provider hiện tại
        public async Task<(List<ProviderNotificationItemDTO> Items, int Total, int UnreadCount)> GetMyNotificationsAsync(CancellationToken ct = default)
        {
            try
            {
                AttachAuth();
                var res = await _http.GetAsync("/api/notification", ct);
                if (!res.IsSuccessStatusCode)
                {
                    await HandleErrorAsync(res, ct);
                }

                var json = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                var total = doc.RootElement.GetProperty("totalCount").GetInt32();
                var unreadCount = doc.RootElement.GetProperty("unreadCount").GetInt32();
                var items = JsonSerializer.Deserialize<List<ProviderNotificationItemDTO>>(
                    doc.RootElement.GetProperty("data").GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                return (items, total, unreadCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return (new List<ProviderNotificationItemDTO>(), 0, 0);
            }
        }

        public async Task<ProviderNotificationItemDTO?> GetAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/notification/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                await HandleErrorAsync(res, ct);
            }
            
            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            return JsonSerializer.Deserialize<ProviderNotificationItemDTO>(
                doc.RootElement.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"/api/notification/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }

        public async Task<object> MarkAsReadAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PutAsync($"/api/notification/{id}/mark-read", null, ct);
            if (!res.IsSuccessStatusCode) await HandleErrorAsync(res, ct);
            
            return await res.Content.ReadFromJsonAsync<object>(cancellationToken: ct)!;
        }

        public async Task<object> MarkAllAsReadAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PutAsync("/api/notification/mark-all-read", null, ct);
            if (!res.IsSuccessStatusCode) await HandleErrorAsync(res, ct);
            
            return await res.Content.ReadFromJsonAsync<object>(cancellationToken: ct)!;
        }
    }
}

