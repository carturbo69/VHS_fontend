using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Payment;

namespace VHS_frontend.Services.Admin
{
    public class PaymentManagementService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        
        public PaymentManagementService(HttpClient http) => _http = http;
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

        // Dashboard
        public async Task<PaymentDashboardDTO?> GetDashboardAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/dashboard", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<PaymentDashboardDTO>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        // Unconfirmed Bookings for Refund
        public async Task<List<UnconfirmedBookingRefundDTO>?> GetUnconfirmedBookingsForRefundAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/unconfirmed-bookings-for-refund", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<UnconfirmedBookingRefundDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        public async Task<bool> ApproveRefundForUnconfirmedBookingAsync(Guid bookingId, string adminNote, CancellationToken ct = default)
        {
            AttachAuth();
            
            var dto = new ApproveRefundRequestDTO
            {
                BookingId = bookingId,
                AdminNote = adminNote
            };
            
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PostAsync("/api/PaymentManagement/approve-refund-unconfirmed", content, ct);
            await HandleErrorAsync(res, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Success ?? false;
        }

        // Pending Provider Payments
        public async Task<List<PendingProviderPaymentDTO>?> GetPendingProviderPaymentsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/pending-provider-payments", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<PendingProviderPaymentDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        public async Task<bool> ApproveProviderPaymentAsync(Guid bookingId, string adminNote, CancellationToken ct = default)
        {
            AttachAuth();
            
            var dto = new ApproveProviderPaymentRequestDTO
            {
                BookingId = bookingId,
                AdminNote = adminNote
            };
            
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PostAsync("/api/PaymentManagement/approve-provider-payment", content, ct);
            await HandleErrorAsync(res, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Success ?? false;
        }

        // Withdrawals
        public async Task<List<ProviderWithdrawalDTO>?> GetPendingWithdrawalsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/withdrawals/pending", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<ProviderWithdrawalDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        public async Task<bool> ApproveWithdrawalAsync(Guid withdrawalId, string action, string adminNote, CancellationToken ct = default)
        {
            AttachAuth();
            
            var dto = new ApproveWithdrawalRequestDTO
            {
                WithdrawalId = withdrawalId,
                Action = action,
                AdminNote = adminNote
            };
            
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PostAsync("/api/PaymentManagement/withdrawals/approve", content, ct);
            await HandleErrorAsync(res, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Success ?? false;
        }

        // Approved/Processed Items (for viewing history)

        /// <summary>
        /// Get approved/completed refunds
        /// </summary>
        public async Task<List<UnconfirmedBookingRefundDTO>?> GetApprovedRefundsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/refunds/approved", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<UnconfirmedBookingRefundDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        /// <summary>
        /// Get approved provider payments
        /// </summary>
        public async Task<List<PendingProviderPaymentDTO>?> GetApprovedProviderPaymentsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/provider-payments/approved", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<PendingProviderPaymentDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        /// <summary>
        /// Get processed withdrawals (Completed/Rejected)
        /// </summary>
        public async Task<List<ProviderWithdrawalDTO>?> GetProcessedWithdrawalsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/PaymentManagement/withdrawals/processed", ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            var response = JsonSerializer.Deserialize<ApiResponse<List<ProviderWithdrawalDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        // Helper class for API responses
        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
        }
    }
}




