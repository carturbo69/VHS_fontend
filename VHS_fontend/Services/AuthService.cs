using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using VHS_fontend.Models;
using VHS_fontend.Models.Account;
using VHS_frontend.Areas.Provider.Models;

namespace VHS_frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl;

        public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;

            var clientBase = _httpClient.BaseAddress?.ToString().TrimEnd('/');
            if (!string.IsNullOrEmpty(clientBase))
                _baseUrl = $"{clientBase}/api/";
            else
                _baseUrl = $"{config["ApiSettings:BaseUrl"]?.TrimEnd('/')}/api/";
        }

        // 🟢 Đăng nhập
        public async Task<LoginRespondDTO?> LoginAsync(LoginDTO dto, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}Auth/login";
            Console.WriteLine($"[LOGIN DEBUG] URL={url}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            try
            {
                var res = await _httpClient.PostAsJsonAsync(url, dto, linkedCt.Token);
                Console.WriteLine($"[AFTER POST] Status={res.StatusCode}");
                var body = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"[AFTER READ] Body={body}");

                if (!res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ERROR] HTTP {(int)res.StatusCode} - {res.ReasonPhrase}");
                    return null;
                }

                var result = await res.Content.ReadFromJsonAsync<LoginRespondDTO>(cancellationToken: linkedCt.Token);
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    // set default header on this HttpClient instance (helps subsequent internal calls)
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                }
                else
                {
                    Console.WriteLine("[ERROR] Không thể parse LoginRespondDTO hoặc token rỗng.");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Login exception: {ex.Message}");
                return null;
            }
        }

        // 🟢 Lấy Provider Profile theo AccountID (gắn token per-request as backup)
        public async Task<ProviderProfileDTO?> GetProviderProfileByAccountIdAsync(Guid accountId)
        {
            try
            {
                var url = $"{_baseUrl}Provider/profile/{accountId}";
                Console.WriteLine($"[PROFILE DEBUG] URL={url}");

                var token = _httpContextAccessor.HttpContext?.Session.GetString("JWToken");
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var res = await _httpClient.SendAsync(request);
                var body = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"[PROFILE DEBUG] Status={res.StatusCode}, Body={body}");

                if (!res.IsSuccessStatusCode) return null;

                return await res.Content.ReadFromJsonAsync<ProviderProfileDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetProviderProfileByAccountIdAsync: {ex.Message}");
                return null;
            }
        }

        // 🟢 Đăng ký tài khoản
        public async Task<RegisterRespondDTO?> RegisterAsync(RegisterDTO dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            try
            {
                var url = $"{_baseUrl.TrimEnd('/')}/api/account/register";
                var response = await _httpClient.PostAsJsonAsync(url, dto, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"Registration failed: {(int)response.StatusCode} {response.ReasonPhrase}";
                    Console.WriteLine($"[WARN] {msg}");
                    return new RegisterRespondDTO { Success = false, Message = msg };
                }

                var result = await response.Content.ReadFromJsonAsync<RegisterRespondDTO>(cancellationToken: ct).ConfigureAwait(false);
                return result ?? new RegisterRespondDTO { Success = false, Message = "Empty response from server." };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[WARN] RegisterAsync canceled.");
                return new RegisterRespondDTO { Success = false, Message = "Request canceled." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] RegisterAsync exception: {ex.Message}");
                return new RegisterRespondDTO { Success = false, Message = ex.Message };
            }
        }

        // DTO nội bộ
        public class RegisterRespondDTO
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public object? Data { get; set; }
        }
    }
}
