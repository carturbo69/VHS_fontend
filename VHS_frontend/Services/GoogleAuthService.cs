using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VHS_frontend.Models.Account;

namespace VHS_frontend.Services
{
    public class GoogleAuthService
    {
        private readonly HttpClient _httpClient;

        public GoogleAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LoginRespondDTO?> LoginWithGoogleAsync(string idToken)
        {
            var request = new GoogleLoginRequest { IdToken = idToken };
            var response = await _httpClient.PostAsJsonAsync("api/auth/google-login", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LoginRespondDTO>();
        }

        public async Task<ReadAccountDTO?> GetAccountInfoAsync(Guid accountId, string jwtToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/account/{accountId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ReadAccountDTO>();
        }

    }
}