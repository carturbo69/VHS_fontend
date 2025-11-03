using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Provider.Models.Withdrawal;

namespace VHS_frontend.Services.Provider
{
    public class ProviderWithdrawalService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        
        public ProviderWithdrawalService(HttpClient http) => _http = http;
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

            res.EnsureSuccessStatusCode();
        }

        public async Task<ProviderBalanceDTO?> GetBalanceAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/provider/balance", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<ProviderBalanceDTO>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        public async Task<bool> RequestWithdrawalAsync(ProviderWithdrawalRequestDTO request, CancellationToken ct = default)
        {
            AttachAuth();
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PostAsync("/api/PaymentManagement/provider/withdrawal/request", content, ct);
            await HandleErrorAsync(res, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Success ?? false;
        }

        public async Task<List<ProviderWithdrawalDTO>?> GetWithdrawalsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/provider/withdrawals", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<ProviderWithdrawalDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
        }
    }
}




