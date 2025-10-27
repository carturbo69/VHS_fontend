using System.Net.Http.Headers;
using VHS_frontend.Areas.Customer.Models.ReviewDTOs;

namespace VHS_frontend.Services.Customer
{
    public class ReviewServiceCustomer
    {
        private readonly HttpClient _httpClient;

        public ReviewServiceCustomer(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // BaseAddress đã set ở Program.cs, ví dụ: https://localhost:7154
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        private void SetAuthHeader(string? jwtToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);
            }
        }

        public async Task<bool> CreateReviewAsync(Guid accountId, CreateReviewDTOs dto, string? jwtToken)
        {
            SetAuthHeader(jwtToken);

            using var form = new MultipartFormDataContent();

            // ----- Trường form thường -----
            form.Add(new StringContent(dto.BookingId.ToString()), nameof(dto.BookingId));
            form.Add(new StringContent(dto.ServiceId.ToString()), nameof(dto.ServiceId));
            form.Add(new StringContent(dto.Rating.ToString()), nameof(dto.Rating));
            if (!string.IsNullOrWhiteSpace(dto.Comment))
                form.Add(new StringContent(dto.Comment), nameof(dto.Comment));

            // ----- File: Dùng ByteArrayContent, KHÔNG tự set Content-Disposition -----
            if (dto.ImageFiles != null && dto.ImageFiles.Count > 0)
            {
                foreach (var file in dto.ImageFiles)
                {
                    if (file == null || file.Length == 0) continue;

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms); // đọc toàn bộ nội dung file
                    var bytes = new ByteArrayContent(ms.ToArray());

                    // Content-Type cho part file
                    var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType;
                    bytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    // QUAN TRỌNG: chỉ cần Add với name + filename -> MultipartFormDataContent tự set đúng header
                    form.Add(bytes, "ImageFiles", Path.GetFileName(file.FileName));
                }
            }

            var url = $"api/reviews/{accountId}";
            using var resp = await _httpClient.PostAsync(url, form);

            if (!resp.IsSuccessStatusCode) return false;

            try
            {
                var payload = await resp.Content.ReadFromJsonAsync<SuccessEnvelope>();
                return payload?.success ?? true;
            }
            catch
            {
                // Backend không trả đúng envelope nhưng status OK -> coi là thành công
                return true;
            }
        }

        // ================== GET MY REVIEWS ==================
        // Trả về (success, data, message) để bạn dễ hiện thông báo UI
        public async Task<(bool success, List<ReviewListItemDto> data, string? message)> GetMyReviewsAsync(
            Guid accountId, string? jwtToken, CancellationToken ct = default)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/reviews/mine/{accountId}";
            using var resp = await _httpClient.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                string? raw = null;
                try { raw = await resp.Content.ReadAsStringAsync(ct); } catch { /* ignore */ }
                return (false, new List<ReviewListItemDto>(), raw);
            }

            try
            {
                var payload = await resp.Content.ReadFromJsonAsync<MyReviewsEnvelope>(cancellationToken: ct);
                if (payload?.success == true && payload.data != null)
                {
                    // Backend đã trả URL tuyệt đối cho avatar, thumbnail và ReviewImageUrls.
                    return (true, payload.data, payload.message);
                }

                return (false, new List<ReviewListItemDto>(), payload?.message ?? "Không lấy được dữ liệu.");
            }
            catch (Exception ex)
            {
                return (false, new List<ReviewListItemDto>(), $"Lỗi parse JSON: {ex.Message}");
            }
        }

        // ================== NEW: EDIT REVIEW ==================
        // map đúng với [HttpPut("{accountId:guid}/edit")] [Consumes("multipart/form-data")]
        public async Task<bool> EditReviewAsync(Guid accountId, EditReviewDto dto, string? jwtToken, CancellationToken ct = default)
        {
            SetAuthHeader(jwtToken);

            using var form = new MultipartFormDataContent();

            // Trường text
            form.Add(new StringContent(dto.ReviewId.ToString()), nameof(dto.ReviewId));
            form.Add(new StringContent(dto.Rating.ToString()), nameof(dto.Rating));
            if (!string.IsNullOrWhiteSpace(dto.Comment))
                form.Add(new StringContent(dto.Comment), nameof(dto.Comment));

            // Nhiều file NewImages
            if (dto.NewImages != null && dto.NewImages.Count > 0)
            {
                foreach (var file in dto.NewImages)
                {
                    if (file == null || file.Length == 0) continue;

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var bytes = new ByteArrayContent(ms.ToArray());

                    var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType;
                    bytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    // tên field phải khớp: "NewImages"
                    form.Add(bytes, nameof(dto.NewImages), Path.GetFileName(file.FileName));
                }
            }

            // Nhiều RemoveImages (chuỗi path)
            if (dto.RemoveImages != null && dto.RemoveImages.Count > 0)
            {
                foreach (var path in dto.RemoveImages.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    form.Add(new StringContent(path), nameof(dto.RemoveImages));
                }
            }

            var url = $"api/reviews/{accountId}/edit";
            using var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = form };
            using var resp = await _httpClient.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode) return false;

            try
            {
                var payload = await resp.Content.ReadFromJsonAsync<SuccessEnvelope>(cancellationToken: ct);
                return payload?.success ?? true;
            }
            catch
            {
                // StatusCode OK nhưng không theo envelope => coi như thành công
                return true;
            }
        }

        public async Task<bool> DeleteReviewAsync(
    Guid accountId,
    Guid reviewId,
    string? jwtToken,
    CancellationToken ct = default)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/reviews/{accountId}/{reviewId}";
            using var resp = await _httpClient.DeleteAsync(url, ct);

            if (!resp.IsSuccessStatusCode) return false;

            try
            {
                var payload = await resp.Content.ReadFromJsonAsync<SuccessEnvelope>(cancellationToken: ct);
                return payload?.success ?? true; // server có trả envelope hoặc không đều coi là OK nếu status OK
            }
            catch
            {
                // Status OK nhưng body không đúng envelope -> vẫn xem là thành công
                return true;
            }
        }


        // ====== Envelopes ======
        private sealed class SuccessEnvelope
        {
            public bool success { get; set; }
            public string? message { get; set; }
        }

        private sealed class MyReviewsEnvelope
        {
            public bool success { get; set; }
            public string? message { get; set; }
            public List<ReviewListItemDto>? data { get; set; }
        }
    }
}
