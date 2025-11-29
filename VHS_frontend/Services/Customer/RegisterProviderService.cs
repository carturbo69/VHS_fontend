using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using VHS_frontend.Areas.Customer.Models.Category;
using VHS_frontend.Areas.Customer.Models.RegisterProvider;

namespace VHS_frontend.Services.Customer
{
    public class RegisterProviderService
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true
        };

        public RegisterProviderService(HttpClient http) => _http = http;

        // (Nếu chưa dùng AuthHeaderHandler) – gọi trước khi call API
        public void SetBearerToken(string token) =>
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ===== CATEGORY =====
        public Task<IReadOnlyList<CategoryDTO>> GetCategoriesAsync(CancellationToken ct = default)
            => GetCategoriesAsync(includeDeleted: false, ct);

        public async Task<IReadOnlyList<CategoryDTO>> GetCategoriesAsync(bool includeDeleted, CancellationToken ct = default)
        {
            var url = $"/api/category?includeDeleted={includeDeleted.ToString().ToLower()}";
            var res = await _http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();
            await using var s = await res.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<List<CategoryDTO>>(s, new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
            return data ?? new();
        }

        // ===== MY PROVIDER (lấy hồ sơ hiện tại của account) =====
        // YÊU CẦU API có endpoint: GET /api/register-provider/me
        public async Task<MyProviderDTO?> GetMyProviderAsync(CancellationToken ct = default)
        {
            // Cache busting để đảm bảo lấy dữ liệu mới nhất
            var url = $"/api/register-provider/me?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var res = await _http.GetAsync(url, ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null; // chưa có hồ sơ
            res.EnsureSuccessStatusCode();

            await using var s = await res.Content.ReadAsStreamAsync(ct);
            var dto = await JsonSerializer.DeserializeAsync<MyProviderDTO>(s, _json, ct);
            return dto;
        }

        // Clear cache method (nếu cần)
        public void ClearCache()
        {
            // Clear any cached provider data
            // Có thể mở rộng để clear localStorage nếu dùng JS
        }

        // ===== REGISTER / UPDATE (multipart forward) =====
        public async Task<RegisterResultVm> RegisterAsync(HttpRequest request, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            string Get(string key) => request.Form[key].FirstOrDefault() ?? string.Empty;

            // BE không còn yêu cầu PROVIDERID khi resubmit — tạo mới sau khi tự xóa hồ sơ Rejected gần nhất

            // Text (trim + optional phone)
            var providerName = Get("ProviderName").Trim();
            var phoneNumber = Get("PhoneNumber").Trim();
            var description = Get("Description");

            // Send PascalCase to match Backend expectations
            content.Add(new StringContent(providerName), "ProviderName");
            if (!string.IsNullOrWhiteSpace(phoneNumber))
                content.Add(new StringContent(phoneNumber), "PhoneNumber");
            content.Add(new StringContent(description), "Description");

            // Terms & Insurance (gộp)
            var termsTitle = Get("TermsInsurance.Title");
            var termsDesc = Get("TermsInsurance.Description");
            content.Add(new StringContent(string.IsNullOrWhiteSpace(termsTitle) ? "Điều khoản" : termsTitle.Trim()), "TermsInsurance.Title");
            content.Add(new StringContent(termsDesc?.Trim() ?? string.Empty), "TermsInsurance.Description");

            // Avatar (1 ảnh)
            if (request.Form.Files["ProfileImage"] is IFormFile avatar && avatar.Length > 0)
            {
                var fc = new StreamContent(avatar.OpenReadStream());
                fc.Headers.ContentType = new MediaTypeHeaderValue(avatar.ContentType);
                content.Add(fc, "ProfileImage", avatar.FileName);
            }

            // Certificates (nhiều block; mỗi block nhiều ảnh)
            var indexes = request.Form.Keys
                .Where(k => k.StartsWith("Certificates[") && k.EndsWith("].CategoryId"))
                .Select(k =>
                {
                    var start = "Certificates[".Length;
                    var end = k.IndexOf(']');
                    return int.Parse(k.AsSpan(start, end - start));
                })
                .Distinct()
                .OrderBy(i => i)
                .ToList();

            foreach (var i in indexes)
            {
                var cat = Get($"Certificates[{i}].CategoryId");
                if (!string.IsNullOrEmpty(cat))
                    content.Add(new StringContent(cat), $"Certificates[{i}].CategoryId");

                // TagIds - send multiple values with SAME KEY (no index) for ASP.NET Core to bind as List
                var tagIdsKey = $"Certificates[{i}].TagIds";
                var tagIds = request.Form[tagIdsKey].Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                foreach (var tagId in tagIds)
                {
                    content.Add(new StringContent(tagId), $"Certificates[{i}].TagIds");
                }

                // KHÔNG GỬI Description - field này không dùng nữa
                // Backend sẽ set Description = null hoặc empty

                // BusinessLicenses files
                var files = request.Form.Files.GetFiles($"Certificates[{i}].BusinessLicenses");
                foreach (var f in files)
                {
                    if (f.Length == 0) continue;
                    var sc = new StreamContent(f.OpenReadStream());
                    sc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
                    content.Add(sc, $"Certificates[{i}].BusinessLicenses", f.FileName);
                }
            }

            // Send to real endpoint
            var endpoint = "/api/register-provider";
            
            Console.WriteLine($"=== SENDING REQUEST TO BACKEND ===");
            Console.WriteLine($"BaseAddress: {_http.BaseAddress}");
            Console.WriteLine($"Endpoint: {endpoint}");
            Console.WriteLine($"Full URL: {_http.BaseAddress}{endpoint}");
            
            var res = await _http.PostAsync(endpoint, content, ct);
            var payload = await res.Content.ReadAsStringAsync(ct);

            // Log chi tiết để debug
            Console.WriteLine($"=== BACKEND RESPONSE ===");
            Console.WriteLine($"API Response Status: {res.StatusCode}");
            Console.WriteLine($"API Response Body: {payload}");

            if (!res.IsSuccessStatusCode)
            {
                string msg = $"API {res.StatusCode}";
                List<ApiErrorItem>? errs = null;
                string? detail = null;
                try
                {
                    using var doc = JsonDocument.Parse(payload);
                    if (doc.RootElement.TryGetProperty("message", out var p1)) msg = p1.GetString() ?? msg;
                    else if (doc.RootElement.TryGetProperty("Message", out var p2)) msg = p2.GetString() ?? msg;
                    if (doc.RootElement.TryGetProperty("detail", out var d1)) detail = d1.GetString();
                    else if (doc.RootElement.TryGetProperty("Detail", out var d2)) detail = d2.GetString();
                    if (doc.RootElement.TryGetProperty("Errors", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        errs = new List<ApiErrorItem>();
                        foreach (var it in arr.EnumerateArray())
                        {
                            errs.Add(new ApiErrorItem
                            {
                                Field = it.TryGetProperty("Field", out var f) ? f.GetString() : null,
                                Code = it.TryGetProperty("Code", out var c) ? c.GetString() : null,
                                Message = it.TryGetProperty("Message", out var mm) ? (mm.GetString() ?? string.Empty) : string.Empty
                            });
                        }
                    }
                }
                catch { if (!string.IsNullOrWhiteSpace(payload)) msg = payload; }

                return new RegisterResultVm { Success = false, Message = msg, Detail = detail, Errors = errs, Raw = payload, StatusCode = (int)res.StatusCode };
            }

            // ======= THÀNH CÔNG: bóc JSON case-insensitive + fallback =======
            try
            {
                using var ok = JsonDocument.Parse(payload);

                // id (nhận cả PROVIDERID và ProviderId)
                Guid id = Guid.Empty;
                if (ok.RootElement.TryGetProperty("PROVIDERID", out var pId) && pId.ValueKind == JsonValueKind.String)
                    id = pId.GetGuid();
                else if (ok.RootElement.TryGetProperty("ProviderId", out var pId2) && pId2.ValueKind == JsonValueKind.String)
                    id = pId2.GetGuid();

                // status (nhận cả STATUS và Status; fallback "Pending")
                string status = "Pending";
                if (ok.RootElement.TryGetProperty("STATUS", out var pSt) && pSt.ValueKind == JsonValueKind.String)
                    status = pSt.GetString() ?? "Pending";
                else if (ok.RootElement.TryGetProperty("Status", out var pSt2) && pSt2.ValueKind == JsonValueKind.String)
                    status = pSt2.GetString() ?? "Pending";

                // Tự tạo DTO trả về cho controller để không bị undefined
                var resp = new RegisterProviderResponseDTO
                {
                    // nếu DTO của bạn dùng property UPPERCASE thì gán vào đúng tên;
                    // nếu DTO dùng camelCase thì đổi tên thuộc tính cho khớp
                    PROVIDERID = id,
                    STATUS = status
                };

                return new RegisterResultVm
                {
                    Success = true,
                    Message = "Đăng ký thành công",
                    Response = resp,
                    Raw = payload,
                    StatusCode = 200
                };
            }
            catch
            {
                // Fallback nữa: nếu parse lỗi, ít nhất trả Pending cho chắc
                var resp = new RegisterProviderResponseDTO
                {
                    PROVIDERID = Guid.Empty, // controller vẫn redirect; nhưng bạn có thể log Raw để kiểm tra
                    STATUS = "Pending"
                };
                return new RegisterResultVm { Success = true, Message = "Đăng ký thành công", Response = resp, Raw = payload, StatusCode = 200 };
            }
        }

        // ===== VM =====
        public sealed class RegisterResultVm
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string? Detail { get; set; }
            public RegisterProviderResponseDTO? Response { get; set; }
            public List<ApiErrorItem>? Errors { get; set; }
            public string Raw { get; set; } = "";
            public int StatusCode { get; set; }
        }

        public sealed class ApiErrorItem
        {
            public string? Field { get; set; }
            public string? Code { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
