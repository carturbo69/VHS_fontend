using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Tag;

namespace VHS_frontend.Services.Admin
{
    public class TagAdminService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public TagAdminService(HttpClient http) => _http = http;

        // Get all tags (without category filter)
        public Task<List<TagDTO>?> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
            => _http.GetFromJsonAsync<List<TagDTO>>($"/api/tag?includeDeleted={includeDeleted.ToString().ToLower()}", _json, ct);

        public Task<List<TagDTO>?> GetByCategoryAsync(Guid categoryId, bool includeDeleted = false, CancellationToken ct = default)
            => _http.GetFromJsonAsync<List<TagDTO>>($"/api/tag?categoryId={categoryId}&includeDeleted={includeDeleted.ToString().ToLower()}", _json, ct);

        public Task<TagDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _http.GetFromJsonAsync<TagDTO>($"/api/tag/{id}", _json, ct);

        public Task<HttpResponseMessage> CreateAsync(TagCreateDTO dto, CancellationToken ct = default)
            => _http.PostAsJsonAsync("/api/tag", dto, _json, ct);

        public Task<HttpResponseMessage> UpdateAsync(Guid id, TagUpdateDTO dto, CancellationToken ct = default)
            => _http.PutAsJsonAsync($"/api/tag/{id}", dto, _json, ct);

        public Task<HttpResponseMessage> DeleteAsync(Guid id, CancellationToken ct = default)
            => _http.DeleteAsync($"/api/tag/{id}", ct);

        public Task<HttpResponseMessage> RestoreAsync(Guid id, CancellationToken ct = default)
            => _http.PostAsync($"/api/tag/{id}/restore", content: null, ct);
    }
}
