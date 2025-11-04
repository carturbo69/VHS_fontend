using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminServiceApprovalController : Controller
    {
        private readonly AdminServiceApprovalService _service;

        public AdminServiceApprovalController(AdminServiceApprovalService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWToken");
            var items = await _service.GetPendingAsync(token);
            ViewData["Title"] = "Duyệt dịch vụ";
            return View(items ?? new List<AdminServiceApprovalService.PendingServiceItem>());
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!Guid.TryParse(id, out var gid)) return RedirectToAction("Index");
            var ok = await _service.ApproveAsync(gid, token);
            TempData[ok ? "Success" : "Error"] = ok ? "Đã duyệt dịch vụ." : "Duyệt thất bại.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string id, string? reason)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!Guid.TryParse(id, out var gid)) return RedirectToAction("Index");
            var ok = await _service.RejectAsync(gid, reason, token);
            TempData[ok ? "Success" : "Error"] = ok ? "Đã từ chối dịch vụ." : "Từ chối thất bại.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Detail(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!Guid.TryParse(id, out var gid)) return RedirectToAction("Index");
            var item = await _service.GetDetailAsync(gid, token);
            if (item == null) { TempData["Error"] = "Không tải được chi tiết dịch vụ."; return RedirectToAction("Index"); }
            ViewData["Title"] = "Chi tiết dịch vụ";
            return View(item);
        }
    }
}


