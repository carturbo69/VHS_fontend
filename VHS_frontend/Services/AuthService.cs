// Services/AuthService.cs
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VHS_frontend.Models;
using VHS_frontend.Models.Account;
using VHS_frontend.Models.Account;

namespace VHS_frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginRespondDTO?> LoginAsync(LoginDTO dto, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync("/api/auth/login", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                // Nếu là Unauthorized (401), throw exception với message từ API
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    try
                    {
                        var errorObj = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                        var message = errorObj.TryGetProperty("Message", out var msgProp) ? msgProp.GetString() : 
                                     (errorObj.TryGetProperty("message", out var msgProp2) ? msgProp2.GetString() : "Tài khoản đã bị ngừng hoạt động.");
                        throw new UnauthorizedAccessException(message ?? "Tài khoản đã bị ngừng hoạt động.");
                    }
                    catch (JsonException)
                    {
                        var errorText = await res.Content.ReadAsStringAsync(ct);
                        throw new UnauthorizedAccessException(errorText ?? "Tài khoản đã bị ngừng hoạt động.");
                    }
                }
                return null;
            }

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
            var res = await _httpClient.PostAsJsonAsync("/api/auth/register", dto, ct);

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

        public async Task<bool> VerifyOTPAsync(string email, string otp, CancellationToken ct = default)
        {
            var dto = new { Email = email, OTP = otp };
            var res = await _httpClient.PostAsJsonAsync("/api/auth/verify-otp", dto, ct);
            return res.IsSuccessStatusCode;
        }

        public async Task<RegisterRespondDTO?> ActivateAccountAsync(string email, string otp, CancellationToken ct = default)
        {
            var dto = new { Email = email, OTP = otp };
            var res = await _httpClient.PostAsJsonAsync("/api/auth/activate-account", dto, ct);
            
            var responseText = await res.Content.ReadAsStringAsync(ct);
            
            if (!res.IsSuccessStatusCode)
            {
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                    var message = errorObj.TryGetProperty("Message", out var msgProp) ? msgProp.GetString() : responseText;
                    return new RegisterRespondDTO { Success = false, Message = message ?? responseText };
                }
                catch
                {
                    return new RegisterRespondDTO { Success = false, Message = responseText };
                }
            }

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                var success = result.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                var message = result.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : 
                             (result.TryGetProperty("Message", out var msgProp2) ? msgProp2.GetString() : "Tài khoản đã được kích hoạt thành công.");
                
                return new RegisterRespondDTO 
                { 
                    Success = success, 
                    Message = message ?? "Tài khoản đã được kích hoạt thành công." 
                };
            }
            catch (Exception ex)
            {
                // Nếu không parse được JSON, coi như thành công nếu status code là 200
                return new RegisterRespondDTO { Success = true, Message = "Tài khoản đã được kích hoạt thành công." };
            }
        }

        public async Task<RegisterRespondDTO?> ResendOTPAsync(string email, CancellationToken ct = default)
        {
            var dto = new { Email = email };
            var res = await _httpClient.PostAsJsonAsync("/api/auth/resend-otp", dto, ct);
            
            if (!res.IsSuccessStatusCode)
            {
                var errText = await res.Content.ReadAsStringAsync(ct);
                return new RegisterRespondDTO { Success = false, Message = errText };
            }

            var result = await res.Content.ReadFromJsonAsync<RegisterRespondDTO>(cancellationToken: ct);
            return result ?? new RegisterRespondDTO { Success = true, Message = "OTP đã được gửi lại." };
        }

        public async Task<RegisterRespondDTO?> SendForgotPasswordOTPAsync(string email, CancellationToken ct = default)
        {
            var dto = new { Email = email };
            var res = await _httpClient.PostAsJsonAsync("/api/auth/forgot-password/send-otp", dto, ct);
            
            if (!res.IsSuccessStatusCode)
            {
                var errText = await res.Content.ReadAsStringAsync(ct);
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(errText);
                    var message = errorObj.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : 
                                 (errorObj.TryGetProperty("Message", out var msgProp2) ? msgProp2.GetString() : errText);
                    return new RegisterRespondDTO { Success = false, Message = message ?? errText };
                }
                catch
                {
                    return new RegisterRespondDTO { Success = false, Message = errText };
                }
            }

            var result = await res.Content.ReadFromJsonAsync<RegisterRespondDTO>(cancellationToken: ct);
            return result ?? new RegisterRespondDTO { Success = true, Message = "Mã OTP đã được gửi đến email của bạn." };
        }

        public async Task<RegisterRespondDTO?> VerifyForgotPasswordOTPAsync(string email, string otp, CancellationToken ct = default)
        {
            var dto = new { Email = email, OTP = otp };
            var res = await _httpClient.PostAsJsonAsync("/api/auth/forgot-password/verify-otp", dto, ct);
            
            var responseText = await res.Content.ReadAsStringAsync(ct);
            
            if (!res.IsSuccessStatusCode)
            {
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                    var message = errorObj.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : responseText;
                    return new RegisterRespondDTO { Success = false, Message = message ?? responseText };
                }
                catch
                {
                    return new RegisterRespondDTO { Success = false, Message = responseText };
                }
            }

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                var success = result.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                var message = result.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "OTP hợp lệ.";
                var token = result.TryGetProperty("token", out var tokenProp) ? tokenProp.GetString() : null;
                
                return new RegisterRespondDTO 
                { 
                    Success = success, 
                    Message = message ?? "OTP hợp lệ.",
                    Data = token // Lưu token vào Data để dùng cho reset password
                };
            }
            catch (Exception ex)
            {
                return new RegisterRespondDTO { Success = true, Message = "OTP hợp lệ." };
            }
        }

        public async Task<RegisterRespondDTO?> ResetPasswordAsync(string email, string resetToken, string newPassword, CancellationToken ct = default)
        {
            var dto = new { Email = email, Token = resetToken, Password = newPassword };
            var res = await _httpClient.PostAsJsonAsync("/api/auth/reset-password", dto, ct);
            
            var responseText = await res.Content.ReadAsStringAsync(ct);
            
            if (!res.IsSuccessStatusCode)
            {
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                    var message = errorObj.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : responseText;
                    return new RegisterRespondDTO { Success = false, Message = message ?? responseText };
                }
                catch
                {
                    return new RegisterRespondDTO { Success = false, Message = responseText };
                }
            }

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                var success = result.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                var message = result.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Mật khẩu đã được đặt lại thành công.";
                
                return new RegisterRespondDTO 
                { 
                    Success = success, 
                    Message = message ?? "Mật khẩu đã được đặt lại thành công."
                };
            }
            catch (Exception ex)
            {
                return new RegisterRespondDTO { Success = true, Message = "Mật khẩu đã được đặt lại thành công." };
            }
        }

        /// <summary>
        /// Lấy ProviderId từ AccountId
        /// </summary>
        public async Task<string?> GetProviderIdByAccountIdAsync(string accountId, string token, CancellationToken ct = default)
        {
            try
            {
                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var apiUrl = $"/api/Provider/get-id-by-account/{accountId}";
                var response = await _httpClient.GetAsync(apiUrl, ct);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GetProviderIdByAccountIdAsync] Failed: {response.StatusCode}");
                    return null;
                }

                // API trả về Guid dạng plain text hoặc JSON
                var content = await response.Content.ReadAsStringAsync(ct);
                
                // Remove quotes nếu có
                content = content.Trim('"', ' ', '\n', '\r');
                
                // Validate Guid
                if (Guid.TryParse(content, out _))
                {
                    return content;
                }

                Console.WriteLine($"[GetProviderIdByAccountIdAsync] Invalid Guid: {content}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetProviderIdByAccountIdAsync] Error: {ex.Message}");
                return null;
            }
        }
    }
}
