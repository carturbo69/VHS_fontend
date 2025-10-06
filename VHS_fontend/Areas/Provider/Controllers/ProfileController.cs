using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var idStr = _http.HttpContext?.Session.GetString("AccountID");
            if (string.IsNullOrEmpty(idStr)) return RedirectToAction("Login", "Account", new { area = "" });

            var accountId = Guid.Parse(idStr);
            var vm = await _profileService.GetProfileAsync(accountId);
            if (vm == null) return RedirectToAction(nameof(Index));

            // ✅ Gán dữ liệu có sẵn vào ViewModel
            var updateVm = new ProviderProfileUpdateViewModel
            {
                ProviderName = vm.ProviderName,
                PhoneNumber = vm.PhoneNumber,
                Description = vm.Description,
                Images = vm.Images
            };

            return View(updateVm);  // <- gửi sang View
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProviderProfileUpdateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var idStr = _http.HttpContext?.Session.GetString("AccountID");
            if (string.IsNullOrEmpty(idStr)) return RedirectToAction("Login", "Account", new { area = "" });

            var accountId = Guid.Parse(idStr);
            var ok = await _profileService.UpdateProfileAsync(accountId, model);

            if (ok)
            {
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Cập nhật thất bại, vui lòng thử lại.");
            return View(model);
        }
    }
}
