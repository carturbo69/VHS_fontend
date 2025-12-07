using System.Net.Http.Headers;
using System.Text.Json;

namespace VHS_frontend.Services.Provider
{
    public class ProviderSettingsService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        private string? _bearer;

        public ProviderSettingsService(HttpClient http) => _http = http;

        public void SetBearerToken(string? token)
        {
            _bearer = token;
        }

        private void AttachAuth()
        {
            // Clear previous authorization
            _http.DefaultRequestHeaders.Authorization = null;
            
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearer);
        }

        /// <summary>
        /// Lấy thời gian hủy mặc định (phút) từ system settings
        /// Endpoint này không yêu cầu Admin role, Provider có thể sử dụng
        /// </summary>
        public async Task<int> GetDefaultAutoCancelMinutesAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                // Gọi endpoint public để lấy default auto-cancel minutes
                var res = await _http.GetAsync("/api/AdminSettings/default-auto-cancel-minutes", ct);
                if (!res.IsSuccessStatusCode) 
                {
                    // Nếu không thành công, trả về mặc định 30 phút
                    return 30;
                }
                
                var response = await res.Content.ReadFromJsonAsync<DefaultAutoCancelMinutesResponse>(_json, ct);
                if (response != null && response.Minutes > 0)
                {
                    return response.Minutes;
                }
                
                return 30; // Mặc định 30 phút
            }
            catch
            {
                return 30; // Mặc định 30 phút
            }
        }
    }

    public class DefaultAutoCancelMinutesResponse
    {
        public int Minutes { get; set; }
    }
}

