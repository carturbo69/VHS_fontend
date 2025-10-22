using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Customer.Models.Notification;

namespace VHS_frontend.Services.Customer
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true
        };

        public NotificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Set Bearer token for authentication
        public void SetBearerToken(string token) =>
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        public async Task<NotificationListResponse> GetNotificationsAsync(string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.GetAsync("/api/notification");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<NotificationListResponse>(jsonContent, _json);
                    return result ?? new NotificationListResponse();
                }
                
                return new NotificationListResponse 
                { 
                    Success = false, 
                    Message = "Không thể lấy danh sách thông báo" 
                };
            }
            catch (Exception ex)
            {
                return new NotificationListResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi: {ex.Message}" 
                };
            }
        }

        public async Task<NotificationListResponse> GetUnreadNotificationsAsync(string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.GetAsync("/api/notification/unread");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<NotificationListResponse>(jsonContent, _json);
                    return result ?? new NotificationListResponse();
                }
                
                return new NotificationListResponse 
                { 
                    Success = false, 
                    Message = "Không thể lấy thông báo chưa đọc" 
                };
            }
            catch (Exception ex)
            {
                return new NotificationListResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi: {ex.Message}" 
                };
            }
        }

        public async Task<NotificationDetailResponse> GetNotificationDetailAsync(string notificationId, string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.GetAsync($"/api/notification/{notificationId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<NotificationDetailResponse>(jsonContent, _json);
                    return result ?? new NotificationDetailResponse();
                }
                
                return new NotificationDetailResponse 
                { 
                    Success = false, 
                    Message = "Không thể lấy chi tiết thông báo" 
                };
            }
            catch (Exception ex)
            {
                return new NotificationDetailResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi: {ex.Message}" 
                };
            }
        }

        public async Task<bool> MarkAsReadAsync(string notificationId, string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.PutAsync($"/api/notification/{notificationId}/mark-read", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.PutAsync("/api/notification/mark-all-read", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId, string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.DeleteAsync($"/api/notification/{notificationId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ClearAllNotificationsAsync(string token)
        {
            try
            {
                SetBearerToken(token);
                var response = await _httpClient.DeleteAsync("/api/notification/clear-all");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetUnreadCountAsync(string token)
        {
            try
            {
                var result = await GetUnreadNotificationsAsync(token);
                return result.Success ? result.UnreadCount : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
