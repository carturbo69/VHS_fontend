using System.Net.Http;
using VHS_frontend.Areas.Provider.Models.Staff;

namespace VHS_frontend.Services.Provider
{
    public class ProviderStaffService
    {
        private readonly HttpClient _httpClient;

        public ProviderStaffService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // 🟢 CREATE STAFF (multipart/form-data)
        public async Task<bool> CreateAsync(Guid providerId, StaffCreateViewModel model)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(model.StaffName ?? ""), "StaffName");
            form.Add(new StringContent(model.Password ?? ""), "Password");
            form.Add(new StringContent(model.CitizenID ?? ""), "CitizenID");

            if (model.FaceImages != null)
            {
                foreach (var file in model.FaceImages)
                    form.Add(new StreamContent(file.OpenReadStream()), "FaceImages", file.FileName);
            }

            if (model.CitizenIDImages != null)
            {
                foreach (var file in model.CitizenIDImages)
                    form.Add(new StreamContent(file.OpenReadStream()), "CitizenIDImages", file.FileName);
            }

            var response = await _httpClient.PostAsync($"api/Staff/provider/{providerId}", form);
            return response.IsSuccessStatusCode;
        }

        // 🟡 UPDATE STAFF (multipart/form-data)
        public async Task<bool> UpdateAsync(Guid id, StaffUpdateViewModel model)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(model.StaffName ?? ""), "StaffName");
            form.Add(new StringContent(model.CitizenID ?? ""), "CitizenID");

            if (model.FaceImages != null)
            {
                foreach (var file in model.FaceImages)
                    form.Add(new StreamContent(file.OpenReadStream()), "FaceImages", file.FileName);
            }

            if (model.CitizenIDImages != null)
            {
                foreach (var file in model.CitizenIDImages)
                    form.Add(new StreamContent(file.OpenReadStream()), "CitizenIDImages", file.FileName);
            }

            var response = await _httpClient.PutAsync($"api/Staff/{id}", form);
            return response.IsSuccessStatusCode;
        }

        // 🟢 Các hàm còn lại giữ nguyên
        public async Task<IEnumerable<StaffReadViewModel>?> GetAllByProviderAsync(Guid providerId)
        {
            var response = await _httpClient.GetAsync($"api/Staff/provider/{providerId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IEnumerable<StaffReadViewModel>>();
        }

        public async Task<StaffReadViewModel?> GetByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"api/Staff/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<StaffReadViewModel>();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/Staff/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnlockAsync(Guid id)
        {
            var response = await _httpClient.PutAsync($"api/Staff/unlock/{id}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<StaffReadViewModel>?> GetLockedByProviderAsync(Guid providerId)
        {
            var response = await _httpClient.GetAsync($"api/Staff/provider/{providerId}/locked");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IEnumerable<StaffReadViewModel>>();
        }
    }
}
