using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Provider;
using VHS_frontend.Areas.Provider.Models.Staff;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class StaffController : Controller
    {
        private readonly ProviderStaffService _staffService;

        public StaffController(ProviderStaffService staffService)
        {
            _staffService = staffService;
        }

        // Danh sách nhân viên (chưa bị khóa)
        public async Task<IActionResult> Index()
        {
            var providerId = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerId)) return RedirectToAction("Login", "Account", new { area = "" });

            var list = await _staffService.GetAllByProviderAsync(Guid.Parse(providerId));
            return View(list ?? new List<StaffReadViewModel>());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var providerId = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerId)) return RedirectToAction("Login", "Account", new { area = "" });

            var ok = await _staffService.CreateAsync(Guid.Parse(providerId), model);
            if (ok) return RedirectToAction("Index");

            ModelState.AddModelError("", "Không thể thêm nhân viên. Vui lòng thử lại.");
            return View(model);
        }

        // GET Edit: nạp dữ liệu cũ
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null) return NotFound();

            var model = new StaffUpdateViewModel
            {
                StaffId = staff.StaffId,
                StaffName = staff.StaffName,
                CitizenID = staff.CitizenID,
                FaceImage = staff.FaceImage?.FirstOrDefault(),
                CitizenIDImage = staff.CitizenIDImage?.FirstOrDefault()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StaffUpdateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var ok = await _staffService.UpdateAsync(model.StaffId, model);
            if (ok) return RedirectToAction("Index");

            ModelState.AddModelError("", "Không thể cập nhật thông tin nhân viên.");
            return View(model);
        }

        // Khóa nhân viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _staffService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        // Danh sách bị khóa
        public async Task<IActionResult> Locked()
        {
            var providerId = HttpContext.Session.GetString("ProviderID");
            if (string.IsNullOrEmpty(providerId)) return RedirectToAction("Login", "Account", new { area = "" });

            var lockedList = await _staffService.GetLockedByProviderAsync(Guid.Parse(providerId));
            return View(lockedList ?? new List<StaffReadViewModel>());
        }

        // Mở khóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(Guid id)
        {
            await _staffService.UnlockAsync(id);
            return RedirectToAction("Locked");
        }
    }
}
