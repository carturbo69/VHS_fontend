using System.Text.Json;
using System.Net.Http.Headers;
using VHS_frontend.Areas.Admin.Models.Customer;

namespace VHS_frontend.Services.Admin
{// </summary>
    public class CustomerAdminService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private string? _bearer;

        public CustomerAdminService(HttpClient http) => _http = http;
        
        public void SetBearerToken(string token) => _bearer = token;

        private void AttachAuth()
        {
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _bearer);
        }

        public async Task<List<CustomerDTO>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
        {
            AttachAuth();
            var url = $"/api/account?includeDeleted={includeDeleted.ToString().ToLower()}&$filter=Role eq 'User'";
            var list = await _http.GetFromJsonAsync<List<CustomerDTO>>(url, _json, ct);
            return list ?? new();
        }

        public Task<CustomerDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.GetFromJsonAsync<CustomerDTO>($"/api/account/{id}", _json, ct);
        }

        public Task<HttpResponseMessage> CreateAsync(CreateCustomerDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.PostAsJsonAsync("/api/account", dto, _json, ct);
        }

        public Task<HttpResponseMessage> UpdateAsync(Guid id, UpdateCustomerDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.PutAsJsonAsync($"/api/account/{id}", dto, _json, ct);
        }

        public Task<HttpResponseMessage> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.DeleteAsync($"/api/account/{id}", ct);
        }

        public Task<HttpResponseMessage> RestoreAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            return _http.PostAsync($"/api/account/{id}/restore", content: null, ct);
        }
    }
}
