using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Category;

namespace VHS_frontend.Services.Admin
{
    // Typed HttpClient: Program.cs sẽ set BaseAddress = Apis:Backend
    public class CategoryAdminService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public CategoryAdminService(HttpClient http) => _http = http;

        public async Task<List<CategoryDTO>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
        {
            var list = await _http.GetFromJsonAsync<List<CategoryDTO>>(
                $"/api/category?includeDeleted={includeDeleted.ToString().ToLower()}",
                _json, ct);
            return list ?? new();
        }

        public Task<CategoryDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _http.GetFromJsonAsync<CategoryDTO>($"/api/category/{id}", _json, ct);

        public Task<HttpResponseMessage> CreateAsync(CategoryCreateDTO dto, CancellationToken ct = default)
            => _http.PostAsJsonAsync("/api/category", dto, _json, ct);

        public Task<HttpResponseMessage> UpdateAsync(Guid id, CategoryUpdateDTO dto, CancellationToken ct = default)
            => _http.PutAsJsonAsync($"/api/category/{id}", dto, _json, ct);

        public Task<HttpResponseMessage> DeleteAsync(Guid id, CancellationToken ct = default)
            => _http.DeleteAsync($"/api/category/{id}", ct);

        public Task<HttpResponseMessage> RestoreAsync(Guid id, CancellationToken ct = default)
            => _http.PostAsync($"/api/category/{id}/restore", content: null, ct);
    }
}
