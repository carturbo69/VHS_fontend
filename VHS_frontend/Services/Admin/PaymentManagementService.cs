using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        // Unconfirmed Bookings for Refund with OData support
        public async Task<List<UnconfirmedBookingRefundDTO>?> GetUnconfirmedBookingsForRefundAsync(
            int? page = null, 
            int? pageSize = null, 
            string? orderBy = null, 
            string? filter = null,
            CancellationToken ct = default)
        {
            AttachAuth();
            
            var queryParams = new List<string>();
            if (page.HasValue && pageSize.HasValue)
            {
                queryParams.Add($"$skip={(page.Value - 1) * pageSize.Value}");
                queryParams.Add($"$top={pageSize.Value}");
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                queryParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
            }
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            
            var url = "/api/PaymentManagement/unconfirmed-bookings-for-refund";
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            
            // Try to parse as OData response first
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    return JsonSerializer.Deserialize<List<UnconfirmedBookingRefundDTO>>(valueArray.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch { }
            
            // Fallback to old format
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

        public async Task<bool> RejectRefundForUnconfirmedBookingAsync(Guid bookingId, string adminNote, CancellationToken ct = default)
        {
            AttachAuth();
            
            var dto = new ApproveRefundRequestDTO
            {
                BookingId = bookingId,
                AdminNote = adminNote
            };
            
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PostAsync("/api/PaymentManagement/reject-refund-unconfirmed", content, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            
            // Chỉ throw exception nếu status code không phải 2xx
            if (!res.IsSuccessStatusCode)
            {
                string msg = "Đã có lỗi xảy ra.";
                try
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        msg = m.GetString() ?? msg;
                }
                catch { /* ignore parse error */ }
                throw new HttpRequestException(msg);
            }
            
            // Parse response (backend trả về { Success, Message, Data } với Success uppercase)
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                
                // Ưu tiên check Success (uppercase) vì backend trả về như vậy
                if (doc.RootElement.TryGetProperty("Success", out var successProp) && successProp.ValueKind == JsonValueKind.True)
                {
                    return true;
                }
                else if (doc.RootElement.TryGetProperty("Success", out var successProp2) && successProp2.ValueKind == JsonValueKind.False)
                {
                    return false;
                }
                // Fallback: check success (lowercase)
                else if (doc.RootElement.TryGetProperty("success", out var successPropLower))
                {
                    if (successPropLower.ValueKind == JsonValueKind.True) return true;
                    if (successPropLower.ValueKind == JsonValueKind.False) return false;
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng vẫn thử deserialize
                Console.WriteLine($"[RejectRefund] Error parsing response: {ex.Message}");
            }
            
            // Fallback: deserialize với case-insensitive
            try
            {
                var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return response?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RejectRefund] Error deserializing response: {ex.Message}, Response: {responseJson}");
                // Nếu không parse được, nhưng status code là 200, coi như thành công
                return res.IsSuccessStatusCode;
            }
        }


        // Withdrawals with OData support
        public async Task<List<ProviderWithdrawalDTO>?> GetPendingWithdrawalsAsync(
            int? page = null, 
            int? pageSize = null, 
            string? orderBy = null, 
            string? filter = null,
            CancellationToken ct = default)
        {
            AttachAuth();
            
            var queryParams = new List<string>();
            if (page.HasValue && pageSize.HasValue)
            {
                queryParams.Add($"$skip={(page.Value - 1) * pageSize.Value}");
                queryParams.Add($"$top={pageSize.Value}");
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                queryParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
            }
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            
            var url = "/api/PaymentManagement/withdrawals/pending";
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            
            // Try to parse as OData response first
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    return JsonSerializer.Deserialize<List<ProviderWithdrawalDTO>>(valueArray.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch { }
            
            // Fallback to old format
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
        /// Get approved/completed refunds with OData support
        /// </summary>
        public async Task<List<UnconfirmedBookingRefundDTO>?> GetApprovedRefundsAsync(
            int? page = null, 
            int? pageSize = null, 
            string? orderBy = null, 
            string? filter = null,
            CancellationToken ct = default)
        {
            AttachAuth();
            
            var queryParams = new List<string>();
            if (page.HasValue && pageSize.HasValue)
            {
                queryParams.Add($"$skip={(page.Value - 1) * pageSize.Value}");
                queryParams.Add($"$top={pageSize.Value}");
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                queryParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
            }
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            
            var url = "/api/PaymentManagement/refunds/approved";
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            
            // Try to parse as OData response first
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    return JsonSerializer.Deserialize<List<UnconfirmedBookingRefundDTO>>(valueArray.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch { }
            
            // Fallback to old format
            var response = JsonSerializer.Deserialize<ApiResponse<List<UnconfirmedBookingRefundDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }


        /// <summary>
        /// Get processed withdrawals (Completed/Rejected) with OData support
        /// </summary>
        public async Task<List<ProviderWithdrawalDTO>?> GetProcessedWithdrawalsAsync(
            int? page = null, 
            int? pageSize = null, 
            string? orderBy = null, 
            string? filter = null,
            CancellationToken ct = default)
        {
            AttachAuth();
            
            var queryParams = new List<string>();
            if (page.HasValue && pageSize.HasValue)
            {
                queryParams.Add($"$skip={(page.Value - 1) * pageSize.Value}");
                queryParams.Add($"$top={pageSize.Value}");
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                queryParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
            }
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            
            var url = "/api/PaymentManagement/withdrawals/processed";
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);
            
            var json = await res.Content.ReadAsStringAsync(ct);
            
            // Try to parse as OData response first
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    return JsonSerializer.Deserialize<List<ProviderWithdrawalDTO>>(valueArray.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch { }
            
            // Fallback to old format
            var response = JsonSerializer.Deserialize<ApiResponse<List<ProviderWithdrawalDTO>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response?.Data;
        }

        /// <summary>
        /// Get total revenue from VNPAY paid payments
        /// </summary>
        public async Task<decimal> GetTotalVNPAYRevenueAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync("/api/PaymentManagement/total-vnpay-revenue", ct);
                
                if (!res.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] API call failed: {res.StatusCode}");
                    var errorContent = await res.Content.ReadAsStringAsync(ct);
                    System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] Error response: {errorContent}");
                    return 0;
                }
                
                var json = await res.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] Response: {json}");
                
                // Parse using ApiResponse pattern like GetDashboardAsync
                var response = JsonSerializer.Deserialize<ApiResponse<TotalVNPAYRevenueDTO>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (response?.Data != null)
                {
                    var revenue = response.Data.TotalRevenue;
                    System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] ✅ Parsed revenue: {revenue:N0}");
                    return revenue;
                }
                
                System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] ❌ Could not parse revenue from response");
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] ❌ Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetTotalVNPAYRevenue] ❌ Stack trace: {ex.StackTrace}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get today revenue from VNPAY paid payments
        /// </summary>
        public async Task<decimal> GetTodayVNPAYRevenueAsync(DateTime? date = null, CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var dateParam = date ?? DateTime.Now;
                var url = $"/api/PaymentManagement/today-vnpay-revenue?date={Uri.EscapeDataString(dateParam.ToString("o"))}";
                var res = await _http.GetAsync(url, ct);
                
                if (!res.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] API call failed: {res.StatusCode}");
                    var errorContent = await res.Content.ReadAsStringAsync(ct);
                    System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] Error response: {errorContent}");
                    return 0;
                }
                
                var json = await res.Content.ReadAsStringAsync(ct);
                System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] Response: {json}");
                
                // Parse using ApiResponse pattern
                var response = JsonSerializer.Deserialize<ApiResponse<TotalVNPAYRevenueDTO>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (response?.Data != null)
                {
                    var revenue = response.Data.TotalRevenue;
                    System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] ✅ Parsed revenue: {revenue:N0}");
                    return revenue;
                }
                
                System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] ❌ Could not parse revenue from response");
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] ❌ Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetTodayVNPAYRevenue] ❌ Stack trace: {ex.StackTrace}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get recent payments (paid/completed) within a date range
        /// </summary>
        public async Task<List<RecentPaymentDTO>?> GetRecentPaymentsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? top = null,
            CancellationToken ct = default)
        {
            AttachAuth();
            
            var queryParams = new List<string>();
            if (fromDate.HasValue)
            {
                queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
            }
            if (toDate.HasValue)
            {
                queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
            }
            if (top.HasValue)
            {
                queryParams.Add($"$top={top.Value}");
            }
            
            var url = "/api/PaymentManagement/recent-payments";
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            try
            {
                var res = await _http.GetAsync(url, ct);
                
                if (!res.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetRecentPayments] API call failed: {res.StatusCode}");
                    return new List<RecentPaymentDTO>();
                }
                
                var json = await res.Content.ReadAsStringAsync(ct);
                
                // Try to parse as OData response first
                try
                {
                    var jsonDoc = JsonDocument.Parse(json);
                    if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                    {
                        return JsonSerializer.Deserialize<List<RecentPaymentDTO>>(valueArray.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
                catch { }
                
                // Fallback to ApiResponse format
                var response = JsonSerializer.Deserialize<ApiResponse<List<RecentPaymentDTO>>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return response?.Data ?? new List<RecentPaymentDTO>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetRecentPayments] Exception: {ex.Message}");
                return new List<RecentPaymentDTO>();
            }
        }
        
        // Helper DTO for VNPAY revenue response
        private class TotalVNPAYRevenueDTO
        {
            public decimal TotalRevenue { get; set; }
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





