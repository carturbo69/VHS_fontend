using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VHS_frontend.Services.Admin
{
    public class AdminSettingsService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        private string? _bearer;

        public AdminSettingsService(HttpClient http) => _http = http;

        public void SetBearerToken(string? token)
        {
            _bearer = token;
        }

        private void AttachAuth()
        {
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearer);
        }

        /// <summary>
        /// Lấy thời gian hủy mặc định (phút)
        /// </summary>
        public async Task<int> GetAutoCancelMinutesAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                // Lấy setting theo key
                var res = await _http.GetAsync("/api/AdminSettings/AutoCancelPendingBookingMinutes", ct);
                if (!res.IsSuccessStatusCode) 
                {
                    // Nếu không tìm thấy, trả về mặc định 30 phút
                    return 30;
                }
                
                var setting = await res.Content.ReadFromJsonAsync<SystemSettingResponse>(_json, ct);
                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    if (int.TryParse(setting.Value, out var minutes) && minutes > 0)
                    {
                        return minutes;
                    }
                }
                
                return 30; // Mặc định 30 phút
            }
            catch
            {
                return 30; // Mặc định 30 phút
            }
        }

        /// <summary>
        /// Lấy setting theo key
        /// </summary>
        public async Task<SystemSettingResponse?> GetSettingByKeyAsync(string key, CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync($"/api/AdminSettings/{Uri.EscapeDataString(key)}", ct);
                if (!res.IsSuccessStatusCode) return null;
                
                return await res.Content.ReadFromJsonAsync<SystemSettingResponse>(_json, ct);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Cập nhật thời gian hủy mặc định (phút)
        /// </summary>
        public async Task<bool> UpdateAutoCancelMinutesAsync(int minutes, CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var requestBody = new { Minutes = minutes };
                var json = JsonSerializer.Serialize(requestBody, _json);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var res = await _http.PostAsync("/api/AdminSettings/auto-cancel-pending-booking", content, ct);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy tất cả settings
        /// </summary>
        public async Task<List<SystemSettingResponse>> GetAllSettingsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync("/api/AdminSettings", ct);
                if (!res.IsSuccessStatusCode) return new List<SystemSettingResponse>();
                
                return await res.Content.ReadFromJsonAsync<List<SystemSettingResponse>>(_json, ct) 
                    ?? new List<SystemSettingResponse>();
            }
            catch
            {
                return new List<SystemSettingResponse>();
            }
        }

        /// <summary>
        /// Lấy giờ giới hạn đặt trước (MinHoursAhead)
        /// </summary>
        public async Task<int> GetMinHoursAheadAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync("/api/AdminSettings/BookingMinHoursAhead", ct);
                if (!res.IsSuccessStatusCode) 
                {
                    return 3; // Mặc định 3 giờ
                }
                
                var setting = await res.Content.ReadFromJsonAsync<SystemSettingResponse>(_json, ct);
                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    if (int.TryParse(setting.Value, out var hours) && hours > 0)
                    {
                        return hours;
                    }
                }
                
                return 3; // Mặc định 3 giờ
            }
            catch
            {
                return 3; // Mặc định 3 giờ
            }
        }

        /// <summary>
        /// Cập nhật giờ giới hạn đặt trước (MinHoursAhead)
        /// </summary>
        public async Task<bool> UpdateMinHoursAheadAsync(int hours, CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var requestBody = new { Hours = hours };
                var json = JsonSerializer.Serialize(requestBody, _json);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var res = await _http.PostAsync("/api/AdminSettings/booking-min-hours-ahead", content, ct);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy ngày giới hạn đặt trước (MaxDaysAhead)
        /// </summary>
        public async Task<int> GetMaxDaysAheadAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync("/api/AdminSettings/BookingMaxDaysAhead", ct);
                if (!res.IsSuccessStatusCode) 
                {
                    return 15; // Mặc định 15 ngày
                }
                
                var setting = await res.Content.ReadFromJsonAsync<SystemSettingResponse>(_json, ct);
                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    if (int.TryParse(setting.Value, out var days) && days > 0)
                    {
                        return days;
                    }
                }
                
                return 15; // Mặc định 15 ngày
            }
            catch
            {
                return 15; // Mặc định 15 ngày
            }
        }

        /// <summary>
        /// Cập nhật ngày giới hạn đặt trước (MaxDaysAhead)
        /// </summary>
        public async Task<bool> UpdateMaxDaysAheadAsync(int days, CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var requestBody = new { Days = days };
                var json = JsonSerializer.Serialize(requestBody, _json);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var res = await _http.PostAsync("/api/AdminSettings/booking-max-days-ahead", content, ct);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class SystemSettingResponse
    {
        public Guid SettingId { get; set; }
        public string Key { get; set; } = null!;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

