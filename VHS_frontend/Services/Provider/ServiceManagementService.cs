using VHS_frontend.Areas.Provider.Models.Service;
using System.Text.Json;

namespace VHS_frontend.Services.Provider
{
    public class ServiceManagementService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public ServiceManagementService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private void SetAuthHeader(string? token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Lấy tất cả dịch vụ của provider
        public async Task<List<ServiceProviderReadDTO>?> GetServicesByProviderAsync(string providerId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"/api/ServiceProvider/provider/{providerId}", ct);
            
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ServiceProviderReadDTO>>>(_json, ct);
            return result?.Data;
        }

        // Lấy chi tiết 1 dịch vụ
        public async Task<ServiceProviderReadDTO?> GetServiceByIdAsync(string serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"/api/ServiceProvider/{serviceId}", ct);
            
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceProviderReadDTO>>(_json, ct);
            return result?.Data;
        }

        // Tạo dịch vụ mới
        public async Task<ApiResponse<ServiceProviderReadDTO>?> CreateServiceAsync(ServiceProviderCreateDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);

            using var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent(dto.ProviderId.ToString()), "ProviderId");
            formContent.Add(new StringContent(dto.CategoryId.ToString()), "CategoryId");
            formContent.Add(new StringContent(dto.Title), "Title");
            formContent.Add(new StringContent(dto.Description ?? ""), "Description");
            formContent.Add(new StringContent(dto.Price.ToString()), "Price");
            formContent.Add(new StringContent(dto.UnitType), "UnitType");
            formContent.Add(new StringContent(dto.BaseUnit.ToString()), "BaseUnit");

            if (dto.Avatar != null)
            {
                using var msA = new MemoryStream();
                await dto.Avatar.CopyToAsync(msA, ct);
                var fileContent = new ByteArrayContent(msA.ToArray());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(dto.Avatar.ContentType ?? "application/octet-stream");
                formContent.Add(fileContent, "Avatar", dto.Avatar.FileName);
            }

            if (dto.Avatar != null)
            {
                using var msA2 = new MemoryStream();
                await dto.Avatar.CopyToAsync(msA2, ct);
                var fileContent = new ByteArrayContent(msA2.ToArray());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(dto.Avatar.ContentType ?? "application/octet-stream");
                formContent.Add(fileContent, "Avatar", dto.Avatar.FileName);
            }

            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var img in dto.Images)
                {
                    if (img == null) continue;
                    using var ms = new MemoryStream();
                    await img.CopyToAsync(ms, ct);
                    var fileContent = new ByteArrayContent(ms.ToArray());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(img.ContentType ?? "application/octet-stream");
                    formContent.Add(fileContent, "Images", img.FileName);
                }
            }

            // Add TagIds
            foreach (var tagId in dto.TagIds)
            {
                formContent.Add(new StringContent(tagId.ToString()), "TagIds");
            }

            // Add OptionIds
            foreach (var optionId in dto.OptionIds)
            {
                formContent.Add(new StringContent(optionId.ToString()), "OptionIds");
            }

            var response = await _httpClient.PostAsync("/api/ServiceProvider", formContent, ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<ServiceProviderReadDTO>>(_json, ct);
        }

        // Cập nhật dịch vụ
        public async Task<ApiResponse<string>?> UpdateServiceAsync(string serviceId, ServiceProviderUpdateDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);

            using var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent(dto.Title), "Title");
            formContent.Add(new StringContent(dto.Description ?? ""), "Description");
            formContent.Add(new StringContent(dto.Price.ToString()), "Price");
            formContent.Add(new StringContent(dto.UnitType), "UnitType");
            formContent.Add(new StringContent(dto.BaseUnit.ToString()), "BaseUnit");
            
            if (!string.IsNullOrEmpty(dto.Status))
                formContent.Add(new StringContent(dto.Status), "Status");

            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var img in dto.Images)
                {
                    if (img == null) continue;
                    using var ms = new MemoryStream();
                    await img.CopyToAsync(ms, ct);
                    var fileContent = new ByteArrayContent(ms.ToArray());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(img.ContentType ?? "application/octet-stream");
                    formContent.Add(fileContent, "Images", img.FileName);
                }
            }

            // Add TagIds
            foreach (var tagId in dto.TagIds)
            {
                formContent.Add(new StringContent(tagId.ToString()), "TagIds");
            }

            // Add OptionIds
            foreach (var optionId in dto.OptionIds)
            {
                formContent.Add(new StringContent(optionId.ToString()), "OptionIds");
            }

            var response = await _httpClient.PutAsync($"/api/ServiceProvider/{serviceId}", formContent, ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_json, ct);
        }

        // Xóa dịch vụ
        public async Task<ApiResponse<string>?> DeleteServiceAsync(string serviceId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.DeleteAsync($"/api/ServiceProvider/{serviceId}", ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_json, ct);
        }

        // Lấy danh sách Categories khả dụng cho Provider
        public async Task<List<CategoryDTO>?> GetAvailableCategoriesAsync(string providerId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"/api/ServiceProvider/categories/available/{providerId}", ct);
            
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDTO>>>(_json, ct);
            return result?.Data;
        }

        // Lấy danh sách Tags theo CategoryId
        public async Task<List<TagDTO>?> GetTagsByCategoryAsync(string categoryId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync($"/api/ServiceProvider/tags/category/{categoryId}", ct);
            
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TagDTO>>>(_json, ct);
            return result?.Data;
        }

        // Lấy tất cả Options
        public async Task<List<OptionDTO>?> GetAllOptionsAsync(string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.GetAsync("/api/ServiceProvider/options/all", ct);
            
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<OptionDTO>>>(_json, ct);
            return result?.Data;
        }

        // Tạo Tag mới
        public async Task<ApiResponse<TagDTO>?> CreateTagAsync(VHS_frontend.Areas.Provider.Models.Tag.TagCreateDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            
            var jsonContent = JsonContent.Create(dto, options: _json);
            var response = await _httpClient.PostAsync("/api/provider/tags", jsonContent, ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TagDTO>>(_json, ct);
        }

        // Tạo Option mới
        public async Task<ApiResponse<OptionDTO>?> CreateOptionAsync(VHS_frontend.Areas.Provider.Models.Option.OptionCreateDTO dto, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            
            var jsonContent = JsonContent.Create(dto, options: _json);
            var response = await _httpClient.PostAsync("/api/provider/options", jsonContent, ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<OptionDTO>>(_json, ct);
        }

        // Xóa Tag
        public async Task<ApiResponse<string>?> DeleteTagAsync(string tagId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.DeleteAsync($"/api/provider/tags/{tagId}", ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_json, ct);
        }

        // Xóa Option
        public async Task<ApiResponse<string>?> DeleteOptionAsync(string optionId, string? token = null, CancellationToken ct = default)
        {
            SetAuthHeader(token);
            var response = await _httpClient.DeleteAsync($"/api/provider/options/{optionId}", ct);
            return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_json, ct);
        }
    }
}

