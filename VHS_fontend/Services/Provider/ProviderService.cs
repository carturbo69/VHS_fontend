using System.Net;
using System.Net.Http.Headers;
using VHS_frontend.Areas.Provider.Models;

namespace VHS_frontend.Services.Provider
{
    public class ProviderService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;

        public ProviderService(HttpClient client, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _client = client;
            _httpContextAccessor = httpContextAccessor;
            _config = config;

            // 🧩 Base URL cấu hình sẵn trong appsettings.json
            var baseUrl = _config["ApiSettings:BaseUrl"]?.TrimEnd('/');
            _client.BaseAddress = new Uri(baseUrl ?? "http://localhost:5154/");

            // 🧩 Bắt buộc header JSON
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // 🧠 Hàm phụ - Gắn token tự động vào mỗi request
        private void AttachToken(HttpRequestMessage request)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // 🧠 Hàm phụ - In log ra console
        private static async Task LogResponseAsync(HttpResponseMessage response, string action)
        {
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[{action}] {response.StatusCode} - {body}");
        }

        // 🟢 Lấy danh sách dịch vụ theo ProviderId
        public async Task<List<ServiceViewModel>> GetAllByProviderAsync(Guid providerId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Service/provider/{providerId}");
            AttachToken(request);

            var response = await _client.SendAsync(request);
            await LogResponseAsync(response, "GET SERVICES");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("⚠️ Token hết hạn hoặc không hợp lệ!");
            }

            if (!response.IsSuccessStatusCode)
                return new List<ServiceViewModel>();

            return await response.Content.ReadFromJsonAsync<List<ServiceViewModel>>() ?? new List<ServiceViewModel>();
        }

        // 🟣 Thêm mới dịch vụ
        public async Task<bool> CreateAsync(ServiceViewModel model, IFormFile? image)
        {
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(model.ProviderId.ToString()), "ProviderId");
            content.Add(new StringContent(model.CategoryId.ToString()), "CategoryId");
            content.Add(new StringContent(model.Title ?? ""), "Title");
            content.Add(new StringContent(model.Description ?? ""), "Description");
            content.Add(new StringContent(model.Price.ToString()), "Price");
            content.Add(new StringContent(model.UnitType ?? ""), "UnitType");
            content.Add(new StringContent(model.BaseUnit.ToString()), "BaseUnit");

            if (image != null)
            {
                var stream = new StreamContent(image.OpenReadStream());
                stream.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                content.Add(stream, "Images", image.FileName);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service") { Content = content };
            AttachToken(request);

            var response = await _client.SendAsync(request);
            await LogResponseAsync(response, "CREATE SERVICE");

            return response.IsSuccessStatusCode;
        }

        // 🟠 Cập nhật dịch vụ
        public async Task<bool> UpdateAsync(Guid id, ServiceViewModel model, IFormFile? image)
        {
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(model.Title ?? ""), "Title");
            content.Add(new StringContent(model.Description ?? ""), "Description");
            content.Add(new StringContent(model.Price.ToString()), "Price");
            content.Add(new StringContent(model.UnitType ?? ""), "UnitType");
            content.Add(new StringContent(model.BaseUnit.ToString()), "BaseUnit");
            content.Add(new StringContent(model.Status ?? "Active"), "Status");

            if (image != null)
            {
                var stream = new StreamContent(image.OpenReadStream());
                stream.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                content.Add(stream, "Images", image.FileName);
            }

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Service/{id}") { Content = content };
            AttachToken(request);

            var response = await _client.SendAsync(request);
            await LogResponseAsync(response, "UPDATE SERVICE");

            return response.IsSuccessStatusCode;
        }

        // 🔴 Xóa dịch vụ
        public async Task<bool> DeleteAsync(Guid id)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Service/{id}");
            AttachToken(request);

            var response = await _client.SendAsync(request);
            await LogResponseAsync(response, "DELETE SERVICE");

            return response.IsSuccessStatusCode;
        }
    }
}
