using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using VHS_frontend.Areas.Admin.Models.RegisterProvider;

namespace VHS_frontend.Services.Admin
{
    public class AdminRegisterProviderService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        private string? _bearer;

        public AdminRegisterProviderService(HttpClient http) => _http = http;

        // Nếu chưa dùng AuthHeaderHandler:
        public void SetBearerToken(string? token)
        {
            _bearer = token;
        }
        
        private void AttachAuth()
        {
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearer);
        }

        public async Task<List<AdminProviderItemDTO>> GetListAsync(string status = "Pending", CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/admin/register-providers?status={status}", ct);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<List<AdminProviderItemDTO>>(_json, ct)) ?? new();
        }

        public async Task<AdminProviderDetailDTO?> GetDetailAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/admin/register-providers/{id}", ct);
            if (!res.IsSuccessStatusCode) return null;

            var dto = await res.Content.ReadFromJsonAsync<AdminProviderDetailDTO>(_json, ct);
            if (dto == null) return null;

            // ===== helper: build absolute bằng API base (HttpClient.BaseAddress)
            string MakeAbs(string? u)
            {
                if (string.IsNullOrWhiteSpace(u)) return string.Empty;
                if (Uri.TryCreate(u, UriKind.Absolute, out _)) return u;        // đã absolute
                var baseUri = _http.BaseAddress ?? new Uri("https://apivhs.cuahangkinhdoanh.com/");
                // chuẩn hóa slash để tránh // hoặc \\
                var rel = u.Replace('\\', '/').TrimStart('/');
                return new Uri(baseUri, rel).ToString();
            }

            // 1) Avatar → absolute theo API base
            if (!string.IsNullOrWhiteSpace(dto.Images))
                dto.Images = MakeAbs(dto.Images);

            // 2) Certificate images (JSON string) → absolute rồi serialize lại
            foreach (var c in dto.Certificates)
            {
                if (string.IsNullOrWhiteSpace(c.Images))
                {
                    c.Images = "[]";
                    continue;
                }

                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(c.Images) ?? new();
                    // normalize & make absolute từng phần tử
                    var abs = list.Select(MakeAbs).ToList();
                    c.Images = JsonSerializer.Serialize(abs);
                }
                catch
                {
                    c.Images = "[]";
                }
            }

            return dto;
        }



        public async Task<bool> ApproveAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PostAsync($"/api/admin/register-providers/{id}/approve", content: null, ct);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> RejectAsync(Guid id, string? reason = null, CancellationToken ct = default)
        {
            AttachAuth();
            var payload = JsonSerializer.Serialize(new { reason }, _json);
            var res = await _http.PostAsync(
                $"/api/admin/register-providers/{id}/reject",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                ct);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCertificateAsync(Guid certificateId, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"/api/admin/certificates/{certificateId}", ct);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveTagFromCertificateAsync(Guid certificateId, Guid tagId, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"/api/admin/certificates/{certificateId}/tags/{tagId}", ct);
            return res.IsSuccessStatusCode;
        }
    }
}
