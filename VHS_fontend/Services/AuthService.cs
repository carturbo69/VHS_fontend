using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using VHS_fontend.Models.Account;
using VHS_fontend.Models;

namespace VHS_frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _settings;

        public AuthService(HttpClient httpClient, IOptions<ApiSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            if (_httpClient.BaseAddress == null)
                _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }

        public async Task<LoginRespondDTO?> LoginAsync(LoginDTO dto)
        {
            var res = await _httpClient.PostAsJsonAsync("auth/login", dto);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<LoginRespondDTO>();
        }

        public async Task<object?> RegisterAsync(RegisterDTO dto)
        {
            var res = await _httpClient.PostAsJsonAsync("auth/register", dto);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<object>();
        }
    }
}
