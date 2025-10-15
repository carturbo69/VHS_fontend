using System.Net.Http.Headers;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Voucher;

namespace VHS_frontend.Services.Admin
{
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

        public async Task<(List<AdminVoucherItemDTO> Items, int Total)> GetListAsync(
            AdminVoucherQuery q, CancellationToken ct = default)
        {
            AttachAuth();

            // ⬇️ đổi sang /api/vouchers (không /v1) + thêm "/" đầu
            var url = $"/api/vouchers?keyword={Uri.EscapeDataString(q.Keyword ?? "")}" +
                      $"&onlyActive={(q.OnlyActive?.ToString().ToLower() ?? "")}" +
                      $"&page={q.Page}&pageSize={q.PageSize}";

            var res = await _http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();

            using var s = await res.Content.ReadAsStreamAsync(ct);
            var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
            var total = doc.RootElement.GetProperty("total").GetInt32();
            var items = JsonSerializer.Deserialize<List<AdminVoucherItemDTO>>(
                doc.RootElement.GetProperty("items").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            return (items, total);
        }

        public async Task<AdminVoucherItemDTO?> GetAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/vouchers/{id}", ct); // ⬅️
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<AdminVoucherItemDTO>(cancellationToken: ct);
        }

        public async Task<AdminVoucherItemDTO> CreateAsync(AdminVoucherEditDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PostAsJsonAsync("/api/vouchers", dto, ct); // ⬅️
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<AdminVoucherItemDTO>(cancellationToken: ct))!;
        }

        public async Task<AdminVoucherItemDTO?> UpdateAsync(Guid id, AdminVoucherEditDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.PutAsJsonAsync($"/api/vouchers/{id}", dto, ct); // ⬅️
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<AdminVoucherItemDTO>(cancellationToken: ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.DeleteAsync($"/api/vouchers/{id}", ct); // ⬅️
            return res.IsSuccessStatusCode;
        }
    }
}
