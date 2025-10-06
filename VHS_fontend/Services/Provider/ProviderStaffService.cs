using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Provider.Models.Staff;

namespace VHS_frontend.Services.Provider
{
    public class ProviderStaffService
    {
        private readonly HttpClient _httpClient;

        public ProviderStaffService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5154/api/");
        }

        // GET: Lấy danh sách staff theo ProviderId
        public async Task<IEnumerable<StaffReadViewModel>?> GetAllByProviderAsync(Guid providerId)
        {
            var response = await _httpClient.GetAsync($"Staff/provider/{providerId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IEnumerable<StaffReadViewModel>>();
        }

        // POST: Tạo staff mới
        public async Task<bool> CreateAsync(Guid providerId, StaffCreateViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"Staff/provider/{providerId}", content);
            return response.IsSuccessStatusCode;
        }

        // PUT: Cập nhật staff
        public async Task<bool> UpdateAsync(Guid id, StaffUpdateViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"Staff/{id}", content);
            return response.IsSuccessStatusCode;
        }

        // DELETE: Xóa staff
        public async Task<bool> DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"Staff/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
