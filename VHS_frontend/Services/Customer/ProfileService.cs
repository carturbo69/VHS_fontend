using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using VHS_frontend.Areas.Customer.Models.Profile;
using VHS_frontend.Services.Customer.Interfaces;

namespace VHS_frontend.Services.Customer
{
    public class ProfileService 
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProfileService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Lấy thông tin profile từ Backend API
        /// Backend tự động tạo User nếu chưa có
        /// </summary>
        public async Task<ViewProfileDTO?> GetProfileAsync(string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/Profile");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<ViewProfileDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return profile;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Cập nhật profile
        /// </summary>
        public async Task<ProfileResponseDTO> UpdateProfileAsync(EditProfileViewModel model, string jwt)
        {
            try
            {
                var dto = new
                {
                    AccountName = model.AccountName,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/Profile")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ProfileResponseDTO { Success = false, Message = jsonString };
                }

                var result = JsonSerializer.Deserialize<ProfileResponseDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ProfileResponseDTO { Success = false, Message = "Không thể xử lý response" };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Request OTP để đổi mật khẩu
        /// </summary>
        public async Task<OTPResponseDTO> RequestPasswordChangeOTPAsync(string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/Profile/request-password-change-otp");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OTPResponseDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new OTPResponseDTO { Success = false, Message = "Không thể xử lý response" };
            }
            catch (Exception ex)
            {
                return new OTPResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public async Task<ProfileResponseDTO> ChangePasswordAsync(ChangePasswordViewModel model, string jwt)
        {
            try
            {
                var dto = new
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword,
                    OTP = model.OTP
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/Profile/change-password")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ProfileResponseDTO { Success = false, Message = jsonString };
                }

                var result = JsonSerializer.Deserialize<ProfileResponseDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ProfileResponseDTO { Success = false, Message = "Không thể xử lý response" };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Request OTP để đổi email
        /// </summary>
        public async Task<OTPResponseDTO> RequestEmailChangeOTPAsync(string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/Profile/request-email-change-otp");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OTPResponseDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new OTPResponseDTO { Success = false, Message = "Không thể xử lý response" };
            }
            catch (Exception ex)
            {
                return new OTPResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Đổi email
        /// </summary>
        public async Task<ProfileResponseDTO> ChangeEmailAsync(ChangeEmailViewModel model, string jwt)
        {
            try
            {
                var dto = new
                {
                    NewEmail = model.NewEmail,
                    OtpCode = model.OTP
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/Profile/change-email")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ProfileResponseDTO { Success = false, Message = jsonString };
                }

                var result = JsonSerializer.Deserialize<ProfileResponseDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ProfileResponseDTO { Success = false, Message = "Không thể xử lý response" };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Upload profile image
        /// </summary>
        public async Task<ProfileResponseDTO> UploadProfileImageAsync(IFormFile image, string jwt)
        {
            try
            {
                using var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                stream.Position = 0;

                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "Image", image.FileName);

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/Profile/upload-image")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ProfileResponseDTO { Success = false, Message = jsonString };
                }
                
                // Backend trả về object trực tiếp, không wrap trong ProfileResponseDTO
                var result = JsonSerializer.Deserialize<JsonElement>(jsonString);
                
                var responseDTO = new ProfileResponseDTO
                {
                    Success = result.TryGetProperty("success", out var success) && success.GetBoolean(),
                    Message = result.TryGetProperty("message", out var message) ? message.GetString() ?? "" : ""
                };
                
                // Extract imageUrl and ImagePath
                object? dataObject = null;
                if (result.TryGetProperty("imageUrl", out var imageUrl))
                {
                    dataObject = new { imageUrl = imageUrl.GetString() };
                }
                else if (result.TryGetProperty("ImagePath", out var imagePath))
                {
                    dataObject = new { ImagePath = imagePath.GetString() };
                }
                
                responseDTO.Data = dataObject;
                
                return responseDTO;
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Delete profile image
        /// </summary>
        public async Task<ProfileResponseDTO> DeleteProfileImageAsync(string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, "/api/Profile/delete-image");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProfileResponseDTO>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ProfileResponseDTO { Success = false, Message = "Không thể xử lý response" };
            }
            catch (Exception ex)
            {
                return new ProfileResponseDTO { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        /// <summary>
        /// Check if profile is complete
        /// </summary>
        public async Task<bool> IsProfileCompleteAsync(string jwt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/Profile/completeness");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(jsonString);
                
                if (result.TryGetProperty("isComplete", out var isComplete))
                {
                    return isComplete.GetBoolean();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

