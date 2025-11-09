using System.Net;
using System.Net.Http.Headers;
using System.Text;

using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using static System.Net.WebRequestMethods;

namespace VHS_frontend.Services.Customer
{
    public class BookingServiceCustomer
    {
        private readonly HttpClient _httpClient;

        public BookingServiceCustomer(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

        /// <summary>
        /// Lấy Term of Service theo ProviderId.
        /// Trả về null nếu backend trả 404 (không có TOS cho provider này).
        /// Ném HttpRequestException nếu status khác thành công/404.
        /// </summary>
        public async Task<TermOfServiceDto?> GetTermOfServiceByProviderIdAsync(
            Guid providerId,
            string? jwtToken = null,
            CancellationToken cancellationToken = default)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/Bookings/provider/{providerId}/term-of-service";
            using var resp = await _httpClient.GetAsync(url, cancellationToken);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<TermOfServiceDto>(cancellationToken: cancellationToken);
            return dto;
        }

        /// <summary>
        /// Gọi API tạo N booking (mỗi service 1 booking) và nhận về list BookingId.
        /// </summary>
        public async Task<CreateManyBookingsResult?> CreateManyBookingsAsync(
            CreateManyBookingsDto dto,
            string? jwtToken = null,
            CancellationToken cancellationToken = default)
        {
            SetAuthHeader(jwtToken);

            var url = "api/Bookings/create-many";

            using var resp = await _httpClient.PostAsJsonAsync(url, dto, cancellationToken);

            // Trả lỗi rõ ràng cho 400
            if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var content = await resp.Content.ReadAsStringAsync(cancellationToken);
                
                // Try to parse JSON error response
                string errorMessage = "Không thể tạo đơn hàng. Vui lòng thử lại.";
                try
                {
                    // Backend might return: { error: "...", message: "..." } or { message: "..." }
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                    if (jsonDoc.RootElement.TryGetProperty("message", out var messageProp))
                    {
                        errorMessage = messageProp.GetString() ?? errorMessage;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        errorMessage = errorProp.GetString() ?? errorMessage;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("Message", out var messageProp2))
                    {
                        errorMessage = messageProp2.GetString() ?? errorMessage;
                    }
                    else if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        errorMessage = jsonDoc.RootElement.GetString() ?? errorMessage;
                    }
                }
                catch
                {
                    // If not JSON, use raw content or default message
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        errorMessage = content;
                    }
                }
                
                throw new HttpRequestException(errorMessage);
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<CreateManyBookingsResult>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Xác nhận thanh toán cho các booking (backend sẽ tự phân bổ voucher giống CreateMany).
        /// </summary>
        public async Task ConfirmPaymentsAsync(
            ConfirmPaymentsDto dto,
            string? jwtToken = null,
            CancellationToken cancellationToken = default)
        {
            SetAuthHeader(jwtToken);

            const string url = "api/Bookings/confirm"; // CHUẨN route ở backend
            using var resp = await _httpClient.PostAsJsonAsync(url, dto, cancellationToken);
            resp.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Map từ BookingViewModel (FE) sang CreateManyBookingsDto (API).
        /// </summary>
        public static CreateManyBookingsDto MapFromViewModel(BookingViewModel vm, Guid accountId)
        {
            var items = new List<CreateBookingItemDto>();

            foreach (var it in (vm.Items ?? new List<BookItem>()))
            {
                // ✅ chỉ dùng OptionIds do trang này post về
                var pickedOptionIds = (it.OptionIds ?? new List<Guid>())
                                        .Where(id => id != Guid.Empty)
                                        .Distinct()
                                        .ToList();

                // (tuỳ chọn) fallback nhẹ từ Options nếu OptionIds trống — nếu bạn muốn “chỉ hiển thị mới lưu”
                // thì KHÔNG làm fallback này.
                // if (pickedOptionIds.Count == 0 && it.Options?.Any() == true)
                //     pickedOptionIds = it.Options.Select(o => o.OptionId).Distinct().ToList();

                items.Add(new CreateBookingItemDto
                {
                    ServiceId = it.ServiceId,
                    BookingTime = it.BookingTime,
                    OptionIds = pickedOptionIds
                });
            }

            return new CreateManyBookingsDto
            {
                AccountId = accountId,
                Address = vm.AddressText ?? string.Empty,
                VoucherId = vm.VoucherId,
                Items = items
            };
        }

        public async Task CancelUnpaidAsync(
    List<Guid> bookingIds,
    string? jwtToken = null,
    CancellationToken cancellationToken = default)
        {
            SetAuthHeader(jwtToken);

            var url = "api/Bookings/cancel-unpaid";
            var payload = new { BookingIds = bookingIds };

            using var resp = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var msg = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(string.IsNullOrWhiteSpace(msg) ? "BadRequest" : msg);
            }
            resp.EnsureSuccessStatusCode();
        }

        // VHS_frontend/Services/Customer/BookingServiceCustomer.cs

        public async Task<ListHistoryBookingServiceDTOs?> GetHistoryByAccountAsync(
            Guid accountId,
            string? jwtToken = null,
            CancellationToken cancellationToken = default)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/Bookings/by-account/{accountId}";
            using var resp = await _httpClient.GetAsync(url, cancellationToken);

            // 404 -> coi như không có dữ liệu
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new ListHistoryBookingServiceDTOs { Items = new() };

            resp.EnsureSuccessStatusCode();
            var dto = await resp.Content.ReadFromJsonAsync<ListHistoryBookingServiceDTOs>(cancellationToken: cancellationToken);
            return dto ?? new ListHistoryBookingServiceDTOs { Items = new() };
        }

        /// <summary>
        /// ✅ NEW: Lấy chi tiết lịch sử của 1 booking theo AccountId và BookingId.
        /// </summary>
        public async Task<HistoryBookingDetailDTO?> GetHistoryDetailAsync(
        Guid accountId,
        Guid bookingId,
        string? jwtToken = null,
        CancellationToken cancellationToken = default)
        {
            SetAuthHeader(jwtToken);

            var url = $"api/Bookings/by-account/{accountId}/bookings/{bookingId}";
            using var resp = await _httpClient.GetAsync(url, cancellationToken);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            // Kiểm tra lỗi trước khi đảm bảo success
            if (!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync(cancellationToken);
                var errorMessage = $"Response status code does not indicate success: {(int)resp.StatusCode} ({resp.StatusCode}).";
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    // Thử parse JSON error message từ backend
                    try
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(errorContent);
                        if (jsonDoc.RootElement.TryGetProperty("message", out var messageProp))
                        {
                            errorMessage = messageProp.GetString() ?? errorMessage;
                        }
                        else if (jsonDoc.RootElement.TryGetProperty("error", out var errorProp))
                        {
                            errorMessage = errorProp.GetString() ?? errorMessage;
                        }
                        else if (jsonDoc.RootElement.TryGetProperty("Message", out var messageProp2))
                        {
                            errorMessage = messageProp2.GetString() ?? errorMessage;
                        }
                    }
                    catch
                    {
                        // Nếu không parse được JSON, sử dụng errorContent trực tiếp (có thể là plain text)
                        if (!string.IsNullOrWhiteSpace(errorContent) && errorContent.Length < 500)
                        {
                            errorMessage = errorContent;
                        }
                    }
                }
                throw new HttpRequestException(errorMessage);
            }

            return await resp.Content.ReadFromJsonAsync<HistoryBookingDetailDTO>(cancellationToken: cancellationToken);
        }


        /// <summary>
        /// ✅ Lấy thông tin yêu cầu huỷ/hoàn tiền cho màn hình CanceledDetail từ endpoint mới.
        /// Sử dụng endpoint: api/Bookings/by-account/{accountId}/bookings/{bookingId}/refund-info
        /// </summary>
        public async Task<CancelBookingRequestDTO?> GetCancelInfoDemoAsync(
            Guid bookingId,
            Guid accountId,
            string? jwtToken = null,
            CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] START - BookingId: {bookingId}, AccountId: {accountId}, HasJWT: {!string.IsNullOrWhiteSpace(jwtToken)}");
            
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] JWT token is null or empty, returning null");
                return null;
            }

            SetAuthHeader(jwtToken);

            // Sử dụng endpoint mới: api/Bookings/by-account/{accountId}/bookings/{bookingId}/refund-info
            var url = $"api/Bookings/by-account/{accountId}/bookings/{bookingId}/refund-info";
            
            System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] Calling URL: {url}");
            System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] BookingId: {bookingId}, AccountId: {accountId}");
            
            try
            {
                using var resp = await _httpClient.GetAsync(url, cancellationToken);
                
                System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] Response Status: {resp.StatusCode}");
                
                // Nếu có lỗi, log và trả về null
                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync(cancellationToken);
                    System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] Error: {resp.StatusCode} - {errorContent}");
                    
                    // Nếu là 404, vẫn trả về object rỗng để frontend có thể hiển thị
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new CancelBookingRequestDTO
                        {
                            BookingId = bookingId,
                            Reason = string.Empty,
                            BankName = string.Empty,
                            AccountHolderName = string.Empty,
                            BankAccountNumber = string.Empty
                        };
                    }
                    
                    return null;
                }

                // Deserialize response
                var refundInfo = await resp.Content.ReadFromJsonAsync<BookingRefundInfoDTO>(
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    }, 
                    cancellationToken: cancellationToken);
                
                if (refundInfo == null)
                    return null;

                // Kiểm tra xem có dữ liệu thực sự không
                // Nếu không có dữ liệu, vẫn trả về object để frontend có thể hiển thị "Chưa có thông tin"
                if (string.IsNullOrWhiteSpace(refundInfo.CancelReason) && 
                    string.IsNullOrWhiteSpace(refundInfo.BankAccountNumber))
                {
                    // Trả về object rỗng thay vì null để frontend có thể hiển thị
                    return new CancelBookingRequestDTO
                    {
                        BookingId = bookingId,
                        Reason = string.Empty,
                        BankName = string.Empty,
                        AccountHolderName = string.Empty,
                        BankAccountNumber = string.Empty
                    };
                }

                return new CancelBookingRequestDTO
                {
                    BookingId = bookingId,
                    Reason = refundInfo.CancelReason ?? string.Empty,
                    BankName = refundInfo.BankName ?? string.Empty,
                    AccountHolderName = refundInfo.AccountHolderName ?? string.Empty,
                    BankAccountNumber = refundInfo.BankAccountNumber ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                // Log lỗi để debug
                System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetCancelInfoDemoAsync] Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        // DTO để deserialize từ endpoint mới
        private class BookingRefundInfoDTO
        {
            public Guid BookingId { get; set; }
            public string CancelReason { get; set; } = string.Empty;
            public string BankName { get; set; } = string.Empty;
            public string AccountHolderName { get; set; } = string.Empty;
            public string BankAccountNumber { get; set; } = string.Empty;
        }

        /// <summary>
        /// Customer cancels booking with refund request
        /// </summary>
        public async Task<bool> CancelBookingWithRefundAsync(
            Guid bookingId,
            string cancelReason,
            string bankAccount,
            string bankName,
            string? accountHolderName,
            string? jwtToken,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
                throw new UnauthorizedAccessException("JWT token is required");

            SetAuthHeader(jwtToken);

            var request = new
            {
                BookingId = bookingId,
                CancelReason = cancelReason,
                BankAccount = bankAccount,
                BankName = bankName,
                AccountHolderName = accountHolderName
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _httpClient.PostAsync("/api/Bookings/cancel-with-refund", content, ct);
            
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                var msg = $"Lỗi hủy đơn ({(int)res.StatusCode} {res.ReasonPhrase}). Body: {body}";
                throw new HttpRequestException(msg);
            }

            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var response = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(
                responseJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return response?.Success ?? false;
        }

        // Hàm này cho phép truyền object có đủ các trường động cho cancel refund
        public async Task<ApiResponse<object>?> CancelBookingWithRefundFullAsync(object model, string? jwtToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
                throw new UnauthorizedAccessException("JWT token is required");
            SetAuthHeader(jwtToken);
            var json = System.Text.Json.JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var res = await _httpClient.PostAsync("/api/Bookings/cancel-with-refund", content, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                var msg = $"Lỗi hủy đơn ({(int)res.StatusCode} {res.ReasonPhrase}). Body: {body}";
                throw new HttpRequestException(msg);
            }
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var response = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(
                responseJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return response;
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
        }

    }
}
