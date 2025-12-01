using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Customer.Models.ReportDTOs;

namespace VHS_frontend.Services.Customer
{
    public class ReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ReportService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Create a new report
        /// </summary>
        public async Task<ReadReportDTO?> CreateReportAsync(CreateReportDTO dto, string jwtToken, CancellationToken ct = default)
        {
            try
            {
                var baseUrl = _configuration["Apis:Backend"] ?? "http://apivhs.cuahangkinhdoanh.com";
                var url = $"{baseUrl}/api/Reports";

                using var content = new MultipartFormDataContent();
                
                // Add form fields
                content.Add(new StringContent(dto.BookingId.ToString()), "BookingId");
                content.Add(new StringContent(((int)dto.ReportType).ToString()), "ReportType");
                content.Add(new StringContent(dto.Title ?? string.Empty), "Title");
                
                if (!string.IsNullOrEmpty(dto.Description))
                {
                    content.Add(new StringContent(dto.Description), "Description");
                }
                
                if (dto.ProviderId.HasValue && dto.ProviderId.Value != Guid.Empty)
                {
                    content.Add(new StringContent(dto.ProviderId.Value.ToString()), "ProviderId");
                }

                // Add file attachments
                if (dto.Attachments != null && dto.Attachments.Any())
                {
                    foreach (var file in dto.Attachments)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var streamContent = new StreamContent(file.OpenReadStream());
                            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                            content.Add(streamContent, "Attachments", file.FileName);
                        }
                    }
                }

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                if (!string.IsNullOrWhiteSpace(jwtToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                }

                var response = await _httpClient.SendAsync(request, ct);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException($"Backend returned {response.StatusCode}: {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                var result = JsonSerializer.Deserialize<ReadReportDTO>(json, options);
                
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get report history with filtering
        /// </summary>
        public async Task<PaginatedReportDTO?> GetReportHistoryAsync(ReportFilterDTO filter, string jwtToken, CancellationToken ct = default)
        {
            var baseUrl = _configuration["Apis:Backend"] ?? "http://apivhs.cuahangkinhdoanh.com";
            var queryParams = new List<string>
            {
                $"Page={filter.Page}",
                $"PageSize={filter.PageSize}"
            };

            if (filter.ReportType.HasValue)
                queryParams.Add($"ReportType={filter.ReportType.Value}");
            
            if (filter.Status.HasValue)
                queryParams.Add($"Status={filter.Status.Value}");
            
            if (filter.FromDate.HasValue)
                queryParams.Add($"FromDate={filter.FromDate.Value:yyyy-MM-dd}");
            
            if (filter.ToDate.HasValue)
                queryParams.Add($"ToDate={filter.ToDate.Value:yyyy-MM-dd}");
            
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(filter.SearchTerm)}");

            var url = $"{baseUrl}/api/Reports/history?{string.Join("&", queryParams)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn.");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<PaginatedReportDTO>(json, options);
        }

        /// <summary>
        /// Get specific report by ID
        /// </summary>
        public async Task<ReadReportDTO?> GetReportByIdAsync(Guid reportId, string jwtToken, CancellationToken ct = default)
        {
            var baseUrl = _configuration["Apis:Backend"] ?? "http://apivhs.cuahangkinhdoanh.com";
            var url = $"{baseUrl}/api/Reports/{reportId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn.");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<ReadReportDTO>(json, options);
        }

        /// <summary>
        /// Check if booking has a report
        /// </summary>
        public async Task<(bool HasReport, ReadReportDTO? Report)> CheckBookingHasReportAsync(Guid bookingId, string jwtToken, CancellationToken ct = default)
        {
            try
            {
                var baseUrl = _configuration["Apis:Backend"] ?? "http://apivhs.cuahangkinhdoanh.com";
                var url = $"{baseUrl}/api/Reports/by-booking/{bookingId}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(jwtToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                }

                var response = await _httpClient.SendAsync(request, ct);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, null);

                if (!response.IsSuccessStatusCode)
                    return (false, null);

                var json = await response.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                
                var result = JsonSerializer.Deserialize<CheckReportResponse>(json, options);
                return (result?.HasReport ?? false, result?.Report);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Get available report types
        /// </summary>
        public Dictionary<ReportTypeEnum, string> GetReportTypes()
        {
            return new Dictionary<ReportTypeEnum, string>
            {
                { ReportTypeEnum.ServiceQuality, "Chất lượng dịch vụ" },
                { ReportTypeEnum.ProviderMisconduct, "Hành vi sai trái của provider" },
                { ReportTypeEnum.StaffMisconduct, "Hành vi sai trái của nhân viên" },
                { ReportTypeEnum.Dispute, "Tranh chấp" },
                { ReportTypeEnum.TechnicalIssue, "Vấn đề kỹ thuật" },
                { ReportTypeEnum.RefundRequest, "Yêu cầu hoàn tiền" },
                { ReportTypeEnum.Other, "Khác" }
            };
        }

        // Helper class for deserializing check report response
        private class CheckReportResponse
        {
            public bool HasReport { get; set; }
            public ReadReportDTO? Report { get; set; }
        }

        /// <summary>
        /// Get available report statuses
        /// </summary>
        public Dictionary<ReportStatusEnum, string> GetReportStatuses()
        {
            return new Dictionary<ReportStatusEnum, string>
            {
                { ReportStatusEnum.Pending, "Chờ xử lý" },
                { ReportStatusEnum.InReview, "Đang xem xét" },
                { ReportStatusEnum.Resolved, "Đã giải quyết" },
                { ReportStatusEnum.Rejected, "Đã từ chối" }
            };
        }
    }
}
