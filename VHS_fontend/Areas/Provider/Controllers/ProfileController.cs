using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProfileController : Controller
    {
        private readonly ProviderProfileService _profileService;
        private readonly IHttpContextAccessor _http;

        public ProfileController(ProviderProfileService profileService, IHttpContextAccessor http)
        {
            _profileService = profileService;
            _http = http;
        }

        // =========================
        // HIỂN THỊ HỒ SƠ PROVIDER
        // =========================
        public async Task<IActionResult> Index()
        {
            var idStr = _http.HttpContext?.Session.GetString("AccountID");
            if (string.IsNullOrEmpty(idStr))
                return RedirectToAction("Login", "Account", new { area = "" });

            var accountId = Guid.Parse(idStr);
            var vm = await _profileService.GetProfileAsync(accountId);
            if (vm == null) return NotFound();

            return View(vm);
        }

        // =========================
        // FORM CHỈNH SỬA HỒ SƠ
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var idStr = _http.HttpContext?.Session.GetString("AccountID");
            if (string.IsNullOrEmpty(idStr))
                return RedirectToAction("Login", "Account", new { area = "" });

            var accountId = Guid.Parse(idStr);
            var vm = await _profileService.GetProfileAsync(accountId);
            if (vm == null)
                return RedirectToAction(nameof(Index));

            var updateVm = new ProviderProfileUpdateViewModel
            {
                ProviderName = vm.ProviderName,
                PhoneNumber = vm.PhoneNumber,
                Description = vm.Description,
                Image = vm.Images
            };

            return View(updateVm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IFormCollection form)
        {
            var idStr = _http.HttpContext?.Session.GetString("AccountID");
            if (string.IsNullOrEmpty(idStr))
                return RedirectToAction("Login", "Account", new { area = "" });

            var accountId = Guid.Parse(idStr);

            // Chuẩn bị dữ liệu form gửi lên BE
            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(form["ProviderName"]), "ProviderName");
            formData.Add(new StringContent(form["PhoneNumber"]), "PhoneNumber");
            formData.Add(new StringContent(form["Description"]), "Description");

            if (form.Files.Count > 0)
            {
                var file = form.Files["ImageFile"];
                if (file != null && file.Length > 0)
                {
                    formData.Add(new StreamContent(file.OpenReadStream()), "ImageFile", file.FileName);
                }
            }

            var ok = await _profileService.UpdateProfileWithFileAsync(accountId, formData);

            Console.WriteLine("---- PUT LOG ----");
            Console.WriteLine($"ProviderName: {form["ProviderName"]}");
            Console.WriteLine($"PhoneNumber: {form["PhoneNumber"]}");
            Console.WriteLine($"Description: {form["Description"]}");
            Console.WriteLine($"Has File: {form.Files.Count > 0}");
            Console.WriteLine($"PUT Result: {ok}");
            Console.WriteLine("-----------------");

            if (ok)
            {
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Cập nhật thất bại!";
            return RedirectToAction(nameof(Edit));
        }

    }
}
