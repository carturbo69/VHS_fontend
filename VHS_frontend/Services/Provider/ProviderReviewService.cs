using VHS_frontend.Areas.Provider.Models.Review;
using System.Text.Json;

namespace VHS_frontend.Services.Provider
{
    public class ProviderReviewService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public ProviderReviewService(HttpClient httpClient)
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

        // Lấy danh sách tất cả reviews của Provider
        public async Task<List<ProviderReviewReadDTO>> GetReviewsByProviderIdAsync(
            string providerId, 
            string? token = null, 
            CancellationToken ct = default)
        {
            try
            {
                SetAuthHeader(token);
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ProviderReviewReadDTO>>>(
                    $"/api/provider/reviews/{providerId}",
                    _json, ct);

                return response?.Data ?? new List<ProviderReviewReadDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetReviewsByProviderIdAsync: {ex.Message}");
                return new List<ProviderReviewReadDTO>();
            }
        }

        // Lấy chi tiết một review
        public async Task<ProviderReviewReadDTO?> GetReviewDetailAsync(
            string reviewId,
            string providerId,
            string? token = null,
            CancellationToken ct = default)
        {
            try
            {
                SetAuthHeader(token);
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<ProviderReviewReadDTO>>(
                    $"/api/provider/reviews/detail/{reviewId}?providerId={providerId}",
                    _json, ct);

                return response?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetReviewDetailAsync: {ex.Message}");
                return null;
            }
        }

        // Gửi phản hồi cho review
        public async Task<HttpResponseMessage> ReplyToReviewAsync(
            string reviewId,
            string providerId,
            ReplyReviewDTO replyDto,
            string? token = null,
            CancellationToken ct = default)
        {
            SetAuthHeader(token);
            return await _httpClient.PostAsJsonAsync(
                $"/api/provider/reviews/reply/{reviewId}?providerId={providerId}",
                replyDto,
                _json, ct);
        }

        // Lấy thống kê reviews
        public async Task<List<ReviewStatisticsDTO>> GetReviewStatisticsAsync(
            string providerId,
            string? token = null,
            CancellationToken ct = default)
        {
            try
            {
                SetAuthHeader(token);
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ReviewStatisticsDTO>>>(
                    $"/api/provider/reviews/statistics/{providerId}",
                    _json, ct);

                return response?.Data ?? new List<ReviewStatisticsDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetReviewStatisticsAsync: {ex.Message}");
                return new List<ReviewStatisticsDTO>();
            }
        }
    }
}

