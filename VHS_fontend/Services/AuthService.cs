// Services/AuthService.cs
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VHS_fontend.Models;
using VHS_fontend.Models.Account;

namespace VHS_frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl = "http://localhost:5154/api/auth/";

        public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginRespondDTO?> LoginAsync(LoginDTO dto, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync($"{_baseUrl}login", dto, ct);
            if (!res.IsSuccessStatusCode) return null;

            return await res.Content.ReadFromJsonAsync<LoginRespondDTO>(cancellationToken: ct);
        }

        //    public async Task<object?> RegisterAsync(RegisterDTO dto, CancellationToken ct = default)
        //    {
        //        var res = await _httpClient.PostAsJsonAsync($"{_baseUrl}register", dto, ct);
        //        if (!res.IsSuccessStatusCode) return null;

        //        // Khuyến nghị tạo DTO cho response đăng ký thay vì object, nhưng giữ nguyên theo code bạn gửi
        //        return await res.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
        //    }
        //}

        public async Task<RegisterRespondDTO?> RegisterAsync(RegisterDTO dto, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync($"{_baseUrl}register", dto, ct);

            // Nếu không 2xx: log & trả object lỗi
            if (!res.IsSuccessStatusCode)
            {
                var errText = await res.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"[RegisterAsync] {res.StatusCode}: {errText}");
                return new RegisterRespondDTO { Success = false, Message = errText };
            }

            var mediaType = res.Content.Headers.ContentType?.MediaType;

            // Nếu là JSON: cố parse
            if (string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var json = await res.Content.ReadFromJsonAsync<RegisterRespondDTO>(cancellationToken: ct);
                    if (json != null) return json;

                    // Nếu server trả JSON khác schema, fallback đọc text
                    var raw = await res.Content.ReadAsStringAsync(ct);
                    return new RegisterRespondDTO { Success = true, Message = raw, Data = null };
                }
                catch (JsonException jx)
                {
                    var body = await res.Content.ReadAsStringAsync(ct);
                    Console.WriteLine($"[RegisterAsync] JSON parse error: {jx.Message}. Body: {body}");
                    return new RegisterRespondDTO { Success = true, Message = body };
                }
            }

            // Nếu không phải JSON (text/plain, text/html,...): đọc text
            var text = await res.Content.ReadAsStringAsync(ct);
            return new RegisterRespondDTO { Success = true, Message = text };
        }

        public class RegisterRespondDTO
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public object? Data { get; set; }
        }
    }
}
