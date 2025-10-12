using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminRegisterProviderController : Controller
    {
        private readonly AdminRegisterProviderService _svc;
        private readonly CategoryAdminService _catSvc;

        public AdminRegisterProviderController(
            AdminRegisterProviderService svc,
            CategoryAdminService catSvc)
        {
            _svc = svc;
            _catSvc = catSvc;
        }

        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _svc.SetBearerToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = "All", CancellationToken ct = default)
        {
            // Mặc định = "All" (hiển thị tất cả)
            AttachBearerIfAny();

            // Backend service có thể hiểu "All" = không filter, nếu không thì bạn map ở service.
            var list = await _svc.GetListAsync(status, ct);

            ViewBag.Status = status; // để View set selected cho dropdown
            return View(list);       // Views/AdminRegisterProvider/Index.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            var dto = await _svc.GetDetailAsync(id, ct);
            if (dto == null)
                return RedirectToAction(nameof(Index));

            // Cách A: nạp map CategoryId -> Name cho View
            var cats = await _catSvc.GetAllAsync(includeDeleted: false, ct);
            ViewBag.CatMap = cats.ToDictionary(x => x.CategoryId, x => x.Name);

            return View(dto);        // Views/AdminRegisterProvider/Detail.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            var ok = await _svc.ApproveAsync(id, ct);
            TempData["AdminMsg"] = ok ? "Đã duyệt hồ sơ." : "Duyệt thất bại.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            var ok = await _svc.RejectAsync(id, reason, ct);
            TempData["AdminMsg"] = ok ? "Đã từ chối hồ sơ." : "Từ chối thất bại.";
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
