// Services/Admin/AdminVoucherService.cs
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Voucher;

namespace VHS_frontend.Services.Admin
{
    public class DuplicateCodeException : Exception
    {
        public DuplicateCodeException(string message) : base(message) { }
    }

    public class ApiBadRequestException : Exception
    {
        public ApiBadRequestException(string message) : base(message) { }
    }

    public class AdminVoucherService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        public AdminVoucherService(HttpClient http) => _http = http;
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

            if (res.StatusCode == HttpStatusCode.Conflict)
                throw new DuplicateCodeException(msg);
            if (res.StatusCode == HttpStatusCode.BadRequest)
                throw new ApiBadRequestException(msg);

            res.EnsureSuccessStatusCode(); // sẽ ném HttpRequestException cho các status khác
        }

        public async Task<(List<AdminVoucherItemDTO> Items, int Total)> GetListAsync(AdminVoucherQuery q, CancellationToken ct = default)
        {
            AttachAuth();
            var url = $"/api/admin/vouchers?keyword={Uri.EscapeDataString(q.Keyword ?? "")}" +
                      $"&onlyActive={(q.OnlyActive?.ToString().ToLower() ?? "")}" +
                      $"&page={q.Page}&pageSize={q.PageSize}";

            var res = await _http.GetAsync(url, ct);
            await HandleErrorAsync(res, ct);

            // đọc toàn bộ nội dung trước, không đọc stream trực tiếp 2 lần
            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var total = doc.RootElement.GetProperty("total").GetInt32();
            var items = JsonSerializer.Deserialize<List<AdminVoucherItemDTO>>(
                doc.RootElement.GetProperty("items").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            return (items, total);
        }


        public async Task<AdminVoucherItemDTO?> GetAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/admin/vouchers/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                await HandleErrorAsync(res, ct);
            }
            return await res.Content.ReadFromJsonAsync<AdminVoucherItemDTO>(cancellationToken: ct);
        }

        public async Task<AdminVoucherItemDTO> CreateAsync(AdminVoucherEditDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PostAsJsonAsync("/api/admin/vouchers", dto, ct);
            if (!res.IsSuccessStatusCode) await HandleErrorAsync(res, ct);
            return (await res.Content.ReadFromJsonAsync<AdminVoucherItemDTO>(cancellationToken: ct))!;
        }

        public async Task<AdminVoucherItemDTO?> UpdateAsync(Guid id, AdminVoucherEditDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PutAsJsonAsync($"/api/admin/vouchers/{id}", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return null;
                await HandleErrorAsync(res, ct);
            }
            return await res.Content.ReadFromJsonAsync<AdminVoucherItemDTO>(cancellationToken: ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"/api/admin/vouchers/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound) return false;
                await HandleErrorAsync(res, ct);
            }
            return true;
        }
    }
}
