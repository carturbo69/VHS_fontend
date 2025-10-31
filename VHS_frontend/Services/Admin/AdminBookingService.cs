using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Booking;

namespace VHS_frontend.Services.Admin
{
    public class AdminBookingService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        private string? _bearer;

        public AdminBookingService(HttpClient http) => _http = http;

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
        /// Lấy thống kê booking/payment cho dashboard
        /// </summary>
        public async Task<AdminBookingStatisticsDTO?> GetStatisticsAsync(
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            CancellationToken ct = default)
        {
            AttachAuth();
            
            var queryParams = new List<string>();
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            
            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            
            try
            {
                var res = await _http.GetAsync($"/api/admin/bookings/statistics{query}", ct);
                if (!res.IsSuccessStatusCode) return null;
                
                return await res.Content.ReadFromJsonAsync<AdminBookingStatisticsDTO>(_json, ct);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy dữ liệu doanh thu theo ngày cho biểu đồ
        /// </summary>
        public async Task<List<RevenueChartDataDTO>> GetRevenueChartAsync(int days = 7, CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync($"/api/admin/bookings/revenue-chart?days={days}", ct);
                if (!res.IsSuccessStatusCode) return new List<RevenueChartDataDTO>();
                
                return await res.Content.ReadFromJsonAsync<List<RevenueChartDataDTO>>(_json, ct) 
                    ?? new List<RevenueChartDataDTO>();
            }
            catch
            {
                return new List<RevenueChartDataDTO>();
            }
        }

        /// <summary>
        /// Lấy số đơn hàng theo giờ trong ngày (24 giờ qua)
        /// </summary>
        public async Task<List<OrdersByHourDTO>> GetOrdersByHourAsync(CancellationToken ct = default)
        {
            AttachAuth();
            
            try
            {
                var res = await _http.GetAsync("/api/admin/bookings/orders-by-hour", ct);
                if (!res.IsSuccessStatusCode) return new List<OrdersByHourDTO>();
                
                return await res.Content.ReadFromJsonAsync<List<OrdersByHourDTO>>(_json, ct) 
                    ?? new List<OrdersByHourDTO>();
            }
            catch
            {
                return new List<OrdersByHourDTO>();
            }
        }

        /// <summary>
        /// Lấy thống kê so với ngày/tuần trước để tính % thay đổi
        /// </summary>
        public async Task<(AdminBookingStatisticsDTO? current, AdminBookingStatisticsDTO? previous)> 
            GetStatisticsWithComparisonAsync(DateTime currentStart, DateTime currentEnd, CancellationToken ct = default)
        {
            var daysDiff = (currentEnd - currentStart).Days;
            var previousStart = currentStart.AddDays(-daysDiff);
            var previousEnd = currentStart.AddTicks(-1);

            var current = await GetStatisticsAsync(currentStart, currentEnd, ct);
            var previous = await GetStatisticsAsync(previousStart, previousEnd, ct);

            return (current, previous);
        }
    }
}

