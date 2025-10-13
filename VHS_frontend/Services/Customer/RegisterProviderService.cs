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
            var res = await _http.GetAsync("/api/register-provider/me", ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null; // chưa có hồ sơ
            res.EnsureSuccessStatusCode();

            await using var s = await res.Content.ReadAsStreamAsync(ct);
            var dto = await JsonSerializer.DeserializeAsync<MyProviderDTO>(s, _json, ct);
            return dto;
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

            content.Add(new StringContent(providerName), "PROVIDERNAME");
            if (!string.IsNullOrWhiteSpace(phoneNumber))
                content.Add(new StringContent(phoneNumber), "PHONENUMBER");
            content.Add(new StringContent(description), "DESCRIPTION");

            // Terms & Insurance (gộp)
            content.Add(new StringContent(Get("TermsInsurance.Title").Trim()), "TERMSINSURANCE.TITLE");
            content.Add(new StringContent(Get("TermsInsurance.Description").Trim()), "TERMSINSURANCE.DESCRIPTION");

            // Avatar (1 ảnh)
            if (request.Form.Files["ProfileImage"] is IFormFile avatar && avatar.Length > 0)
            {
                var fc = new StreamContent(avatar.OpenReadStream());
                fc.Headers.ContentType = new MediaTypeHeaderValue(avatar.ContentType);
                content.Add(fc, "PROFILEIMAGE", avatar.FileName);
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
                    content.Add(new StringContent(cat), $"CERTIFICATES[{i}].CATEGORYID");

                var cdesc = Get($"Certificates[{i}].Description");
                content.Add(new StringContent(cdesc), $"CERTIFICATES[{i}].DESCRIPTION");

                var files = request.Form.Files.GetFiles($"Certificates[{i}].BusinessLicenses");
                foreach (var f in files)
                {
                    if (f.Length == 0) continue;
                    var sc = new StreamContent(f.OpenReadStream());
                    sc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
                    content.Add(sc, $"CERTIFICATES[{i}].BUSINESSLICENSES", f.FileName);
                }
            }

            var res = await _http.PostAsync("/api/register-provider", content, ct);
            var payload = await res.Content.ReadAsStringAsync(ct);


            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                string msg = $"API {res.StatusCode}";
                List<ApiErrorItem>? errs = null;
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var p1)) msg = p1.GetString() ?? msg;
                    else if (doc.RootElement.TryGetProperty("Message", out var p2)) msg = p2.GetString() ?? msg;
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
                catch { if (!string.IsNullOrWhiteSpace(body)) msg = body; }

                return new RegisterResultVm { Success = false, Message = msg, Errors = errs, Raw = body, StatusCode = (int)res.StatusCode };
            }

            // ======= THÀNH CÔNG: bóc JSON case-insensitive + fallback =======
            try
            {
                using var ok = JsonDocument.Parse(body);

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
                    Raw = body,
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
                return new RegisterResultVm { Success = true, Message = "Đăng ký thành công", Response = resp, Raw = body, StatusCode = 200 };
            }
        }

        // ===== VM =====
        public sealed class RegisterResultVm
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
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
