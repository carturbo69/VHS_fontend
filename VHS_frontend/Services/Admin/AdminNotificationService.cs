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
            
            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAccountsAsync: {ex.Message}");
                throw;
            }
        }

        // GET: Lấy số lượng thông báo chưa đọc (chỉ thông báo nhận được - ReceiverRole = Admin)
        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                AttachAuth();
                // Lấy tất cả thông báo, sau đó filter trên client side giống như Index view
                var url = "/api/notification/admin/all";
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to get unread count: {res.StatusCode}");
                    return 0;
                }
                
                var json = await res.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                // Lấy data array
                if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Deserialize để filter
                    var notifications = System.Text.Json.JsonSerializer.Deserialize<List<AdminNotificationItemDTO>>(
                        dataElement.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    
                    // Filter: ReceiverRole == "Admin" && IsRead != true (giống logic trong Index view)
                    var unreadCount = notifications.Count(n => 
                        n.ReceiverRole == "Admin" && (n.IsRead == null || n.IsRead == false));
                    
                    Console.WriteLine($"Found {unreadCount} unread notifications for Admin (Total: {notifications.Count})");
                    return unreadCount;
                }
                
                Console.WriteLine($"Warning: Could not find data array in response: {json}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return 0;
            }
        }

        // GET: Lấy danh sách thông báo chưa đọc (chỉ thông báo nhận được - ReceiverRole = Admin)
        public async Task<List<AdminNotificationDTO>> GetUnreadNotificationsAsync()
        {
            try
            {
                AttachAuth();
                // Lấy tất cả thông báo, sau đó filter trên client side giống như Index view
                var url = "/api/notification/admin/all";
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to get unread notifications: {res.StatusCode}");
                    return new List<AdminNotificationDTO>();
                }
                
                var json = await res.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    // Deserialize sang AdminNotificationItemDTO trước
                    var allNotifications = System.Text.Json.JsonSerializer.Deserialize<List<AdminNotificationItemDTO>>(
                        dataElement.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    
                    // Filter: ReceiverRole == "Admin" && IsRead != true
                    var unreadNotifications = allNotifications
                        .Where(n => n.ReceiverRole == "Admin" && (n.IsRead == null || n.IsRead == false))
                        .OrderByDescending(n => n.CreatedAt)
                        .Take(10) // Chỉ lấy 10 thông báo gần nhất cho dropdown
                        .ToList();
                    
                    // Map sang AdminNotificationDTO
                    return unreadNotifications.Select(n => new AdminNotificationDTO
                    {
                        NotificationId = n.NotificationId,
                        AccountReceivedId = n.AccountReceivedId,
                        ReceiverRole = n.ReceiverRole,
                        NotificationType = n.NotificationType,
                        Content = n.Content,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReceiverName = n.ReceiverName,
                        ReceiverEmail = n.ReceiverEmail
                    }).ToList();
                }
                
                return new List<AdminNotificationDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread notifications: {ex.Message}");
                return new List<AdminNotificationDTO>();
            }
        }

        // DELETE: Xóa các thông báo đã gửi (User/Provider) cũ hơn 1 tuần
        public async Task<int> DeleteOldSentNotificationsAsync(CancellationToken ct = default)
        {
            try
            {
                AttachAuth();
                var url = "/api/notification/admin/all";
                var res = await _http.GetAsync(url, ct);
                if (!res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to get notifications for cleanup: {res.StatusCode}");
                    return 0;
                }
                
                var json = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    return 0;
                }
                
                var allNotifications = JsonSerializer.Deserialize<List<AdminNotificationItemDTO>>(
                    dataElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                
                // Lọc các thông báo đã gửi (User/Provider) cũ hơn 7 ngày (từ thời điểm tạo)
                // Tính từ thời điểm hiện tại, các thông báo có CreatedAt < 7 ngày trước sẽ bị xóa
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var oldSentNotifications = allNotifications
                    .Where(n => (n.ReceiverRole == "User" || n.ReceiverRole == "Provider") 
                        && n.CreatedAt.HasValue 
                        && n.CreatedAt.Value.ToUniversalTime() < sevenDaysAgo)
                    .ToList();
                
                Console.WriteLine($"Tìm thấy {oldSentNotifications.Count} thông báo đã gửi cũ hơn 7 ngày (từ {sevenDaysAgo:yyyy-MM-dd HH:mm:ss} UTC)");
                
                int deletedCount = 0;
                foreach (var notification in oldSentNotifications)
                {
                    try
                    {
                        var deleteRes = await _http.DeleteAsync($"/api/notification/{notification.NotificationId}", ct);
                        if (deleteRes.IsSuccessStatusCode)
                        {
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting notification {notification.NotificationId}: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"Deleted {deletedCount} old sent notifications (older than 1 week)");
                return deletedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting old sent notifications: {ex.Message}");
                return 0;
            }
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
