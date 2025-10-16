using System.Net.Http.Json;
using VHS_frontend.Areas.Provider.Models;

namespace VHS_frontend.Services.Provider
{
    public class ProviderProfileService
    {
        private readonly HttpClient _http;

        public ProviderProfileService(HttpClient http)
        {
            _http = http;
        }

        // GET: lấy hồ sơ Provider
        public async Task<ProviderProfileReadViewModel?> GetProfileAsync(Guid accountId)
        {
            return await _http.GetFromJsonAsync<ProviderProfileReadViewModel>(
                $"api/Provider/profile/{accountId}");
        }

        // PUT: cập nhật hồ sơ Provider
        public async Task<bool> UpdateProfileWithFileAsync(Guid accountId, MultipartFormDataContent formData)
        {
            var res = await _http.PutAsync($"api/Provider/profile/{accountId}", formData);
            if (!res.IsSuccessStatusCode)
            {
                var error = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"[UpdateProfileWithFileAsync] Error: {res.StatusCode} - {error}");
            }
            return res.IsSuccessStatusCode;
        }
    }
}
