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

        // Giữ nguyên key name (API đang trả UPPERCASE cho Register response),
        // đồng thời case-insensitive để an toàn nếu backend đổi về PascalCase.
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true
        };

        public RegisterProviderService(HttpClient http) => _http = http;

        // Nếu API cần Bearer token:
        public void SetBearerToken(string token) =>
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ===== CATEGORY =====
        // Mặc định: chỉ lấy những Category còn hoạt động (includeDeleted=false)
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

        // ===== REGISTER PROVIDER =====
        // Forward nguyên form multipart sang API /api/register-provider
        public async Task<RegisterResultVm> RegisterAsync(HttpRequest request, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();

            string Get(string key) => request.Form[key].FirstOrDefault() ?? string.Empty;

            // Text fields (map đúng key UPPERCASE theo DTO backend)
            content.Add(new StringContent(Get("ProviderName")), "PROVIDERNAME");
            content.Add(new StringContent(Get("PhoneNumber")), "PHONENUMBER");
            content.Add(new StringContent(Get("Description")), "DESCRIPTION");

            // TermsInsurance (gộp điều khoản & bảo hiểm)
            content.Add(new StringContent(Get("TermsInsurance.Title")), "TERMSINSURANCE.TITLE");
            content.Add(new StringContent(Get("TermsInsurance.Description")), "TERMSINSURANCE.DESCRIPTION");

            // Avatar (1 ảnh)
            if (request.Form.Files["ProfileImage"] is IFormFile avatar && avatar.Length > 0)
            {
                var fc = new StreamContent(avatar.OpenReadStream());
                fc.Headers.ContentType = new MediaTypeHeaderValue(avatar.ContentType);
                content.Add(fc, "PROFILEIMAGE", avatar.FileName);
            }

            // Certificates (nhiều block; mỗi block nhiều ảnh BusinessLicenses)
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

            if (!res.IsSuccessStatusCode)
            {
                // cố gắng bóc message chi tiết từ body
                string msg = $"API {res.StatusCode}";
                try
                {
                    using var doc = JsonDocument.Parse(payload);
                    if (doc.RootElement.TryGetProperty("Message", out var m))
                        msg = m.GetString() ?? msg;
                }
                catch { /* ignore parse error */ }

                return new RegisterResultVm { Success = false, Message = msg, Raw = payload };
            }

            var data = JsonSerializer.Deserialize<RegisterProviderResponseDTO>(payload, _json);
            return new RegisterResultVm
            {
                Success = true,
                Message = "Đăng ký thành công",
                Response = data!,
                Raw = payload
            };
        }

        // ===== VM cho FE =====
        public sealed class RegisterResultVm
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public RegisterProviderResponseDTO? Response { get; set; }
            public string Raw { get; set; } = "";
        }
    }
}
