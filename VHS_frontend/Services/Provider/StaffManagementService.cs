using VHS_frontend.Areas.Provider.Models.Staff;
using System.Text.Json;
using System.Net.Http;

namespace VHS_frontend.Services.Provider
{
    public class StaffManagementService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public StaffManagementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private void SetAuthHeader(string? token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<StaffDTO>?> GetStaffByProviderAsync(string providerId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.GetFromJsonAsync<List<StaffDTO>>(
                $"/api/staff/provider/{providerId}", 
                _json, ct);
        }

        public async Task<StaffDTO?> GetStaffByIdAsync(string staffId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.GetFromJsonAsync<StaffDTO>(
                $"/api/staff/{staffId}", 
                _json, ct);
        }

        public async Task<HttpResponseMessage> CreateStaffAsync(string providerId, object staffData, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            
            // Check if staffData is MultipartFormDataContent
            if (staffData is MultipartFormDataContent formData)
            {
                return await _httpClient.PostAsync(
                    $"/api/staff/provider/{providerId}", 
                    formData, ct);
            }
            else
            {
                // Fallback to JSON for other data types
                return await _httpClient.PostAsJsonAsync(
                    $"/api/staff/provider/{providerId}", 
                    staffData, _json, ct);
            }
        }

        public async Task<HttpResponseMessage> UpdateStaffAsync(string staffId, object staffData, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            
            // Check if staffData is MultipartFormDataContent
            if (staffData is MultipartFormDataContent formData)
            {
                return await _httpClient.PutAsync(
                    $"/api/staff/{staffId}", 
                    formData, ct);
            }
            else
            {
                // Fallback to JSON for other data types
                return await _httpClient.PutAsJsonAsync(
                    $"/api/staff/{staffId}", 
                    staffData, _json, ct);
            }
        }


        public async Task<HttpResponseMessage> GetProviderIdFromAccountId(string accountId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.GetAsync($"/api/Provider/get-id-by-account/{accountId}", ct);
        }

        public async Task<HttpResponseMessage> LockStaffAsync(string staffId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.PostAsync($"/api/staff/{staffId}/lock", null, ct);
        }

        public async Task<HttpResponseMessage> UnlockStaffAsync(string staffId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.PostAsync($"/api/staff/{staffId}/unlock", null, ct);
        }

        // ✨ MỚI: Lấy lịch làm việc tuần của staff
        public async Task<HttpResponseMessage> GetWeeklyScheduleAsync(
            string staffId, 
            DateTime weekStart, 
            string? token = null, 
            CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var weekStartStr = weekStart.ToString("yyyy-MM-dd");
            return await _httpClient.GetAsync(
                $"/api/provider/staff/{staffId}/schedule?weekStart={weekStartStr}", 
                ct);
        }

        // 🔑 Cập nhật mật khẩu cho Staff
        public async Task<HttpResponseMessage> UpdateStaffPasswordAsync(
            string staffId, 
            StaffUpdatePasswordDTO dto, 
            string? token = null, 
            CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var json = JsonSerializer.Serialize(dto, _json);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            return await _httpClient.PutAsync(
                $"/api/staff/{staffId}/password", 
                content, ct);
        }
    }
}
