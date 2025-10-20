// Services/Admin/AdminNotificationService.cs
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Notification;

namespace VHS_frontend.Services.Admin
{
    public class AdminNotificationService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        public AdminNotificationService(HttpClient http) => _http = http;
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

        public async Task<(List<AdminNotificationItemDTO> Items, int Total)> GetListAsync(AdminNotificationQuery q, CancellationToken ct = default)
        {
            AttachAuth();
            var url = $"/api/notification/admin/all";
            
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(q.Keyword))
                queryParams.Add($"keyword={Uri.EscapeDataString(q.Keyword)}");
            if (!string.IsNullOrWhiteSpace(q.Role))
                queryParams.Add($"role={Uri.EscapeDataString(q.Role)}");
            if (!string.IsNullOrWhiteSpace(q.NotificationType))
                queryParams.Add($"notificationType={Uri.EscapeDataString(q.NotificationType)}");
            if (q.IsRead.HasValue)
                queryParams.Add($"isRead={q.IsRead.Value}");
            
            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var total = doc.RootElement.GetProperty("totalCount").GetInt32();
            var items = JsonSerializer.Deserialize<List<AdminNotificationItemDTO>>(
                doc.RootElement.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            return (items, total);
        }

        public async Task<AdminNotificationItemDTO?> GetAsync(Guid id, CancellationToken ct = default)
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
            
            return JsonSerializer.Deserialize<AdminNotificationItemDTO>(
                doc.RootElement.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<AdminNotificationItemDTO> CreateAsync(AdminNotificationCreateDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PostAsJsonAsync("/api/notification/create", dto, ct);
            if (!res.IsSuccessStatusCode) await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            return JsonSerializer.Deserialize<AdminNotificationItemDTO>(
                doc.RootElement.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<object> SendToRoleAsync(AdminNotificationSendToRoleDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PostAsJsonAsync("/api/notification/send-to-role", dto, ct);
            if (!res.IsSuccessStatusCode) await HandleErrorAsync(res, ct);
            
            return await res.Content.ReadFromJsonAsync<object>(cancellationToken: ct)!;
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

        public async Task<List<AccountItemDTO>> GetAccountsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var url = "/api/account/simple?includeDeleted=false";
            Console.WriteLine($"Calling API: {url}");
            
            var res = await _http.GetAsync(url, ct);
            Console.WriteLine($"Response status: {res.StatusCode}");
            
            if (!res.IsSuccessStatusCode) 
            {
                var errorContent = await res.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"Error content: {errorContent}");
                await HandleErrorAsync(res, ct);
            }
            
            var json = await res.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Response content: {json}");
            
            using var doc = JsonDocument.Parse(json);
            
            return JsonSerializer.Deserialize<List<AccountItemDTO>>(
                doc.RootElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
    }

    public class AccountItemDTO
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
