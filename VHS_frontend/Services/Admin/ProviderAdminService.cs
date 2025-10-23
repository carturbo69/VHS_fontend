using System.Text.Json;
using System.Net.Http.Headers;
using VHS_frontend.Areas.Admin.Models.Provider;

namespace VHS_frontend.Services.Admin
{
    /// <summary>
    /// Service cho trang quản lý Provider: chỉ lấy tài khoản có Role = 'Provider'.
    /// Chức năng: xem danh sách, xem chi tiết, xoá (soft-delete), khôi phục.
    /// Không có tạo/sửa.
    /// </summary>
    public class ProviderAdminService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private string? _bearer;

        public ProviderAdminService(HttpClient http) => _http = http;
        
        public void SetBearerToken(string token) => _bearer = token;

        private void AttachAuth()
        {
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _bearer);
        }

        /// <summary>
        /// Lấy toàn bộ provider. includeDeleted = true để hiện cả bản ghi đã ẩn.
        /// Dùng OData filter giống Customer: $filter=Role eq 'Provider'
        /// </summary>
        public async Task<List<ProviderDTO>> GetAllAsync(
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            AttachAuth();
            var url =
                $"/api/account?includeDeleted={includeDeleted.ToString().ToLower()}" +
                $"&$filter=Role eq 'Provider'";

            var list = await _http.GetFromJsonAsync<List<ProviderDTO>>(url, _json, ct);
            return list ?? new();
        }

        /// <summary>
        /// Xem chi tiết 1 provider theo Id.
        /// </summary>
        public Task<ProviderDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.GetFromJsonAsync<ProviderDTO>($"/api/account/{id}", _json, ct);
        }

        /// <summary>
        /// Soft-delete (ẩn) provider.
        /// </summary>
        public Task<HttpResponseMessage> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.DeleteAsync($"/api/account/{id}", ct);
        }

        /// <summary>
        /// Khôi phục provider đã bị ẩn.
        /// </summary>
        public Task<HttpResponseMessage> RestoreAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.PostAsync($"/api/account/{id}/restore", content: null, ct);
        }
    }
}
