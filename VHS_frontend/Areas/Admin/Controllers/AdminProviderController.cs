using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Provider;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminProviderController : Controller
    {
        private readonly ProviderAdminService _svc;

        public AdminProviderController(ProviderAdminService svc)
        {
            _svc = svc;
        }

        // GET: /Admin/AdminProvider?includeDeleted=false
        [HttpGet]
        public async Task<IActionResult> Index(bool includeDeleted = false, CancellationToken ct = default)
        {
            var list = await _svc.GetAllAsync(includeDeleted, ct);
            ViewData["IncludeDeleted"] = includeDeleted;
            return View(list); // View: Areas/Admin/Views/AdminProvider/Index.cshtml (model: List<ProviderDTO>)
        }

        // GET: /Admin/AdminProvider/Get?id={guid}
        // Trả JSON phục vụ modal "Xem"
        [HttpGet]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        {
            var dto = await _svc.GetByIdAsync(id, ct);
            if (dto == null) return NotFound("Không tìm thấy Provider.");
            return Json(dto);
        }

        // DELETE: /Admin/AdminProvider/Delete?id={guid}
        // Soft-delete (ẩn)
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            var res = await _svc.DeleteAsync(id, ct);
            if (res.IsSuccessStatusCode) return NoContent();

            var msg = await SafeReadAsync(res, ct) ?? "Xoá thất bại.";
            return BadRequest(msg);
        }

        // POST: /Admin/AdminProvider/Restore?id={guid}
        [HttpPost]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        {
            var res = await _svc.RestoreAsync(id, ct);
            if (res.IsSuccessStatusCode) return NoContent();

            var msg = await SafeReadAsync(res, ct) ?? "Khôi phục thất bại.";
            return BadRequest(msg);
        }

        // Helper: đọc body lỗi (nếu có) một cách an toàn
        private static async Task<string?> SafeReadAsync(HttpResponseMessage res, CancellationToken ct)
        {
            try
            {
                return res.Content is null ? null : (await res.Content.ReadAsStringAsync(ct));
            }
            catch
            {
                return null;
            }
        }
    }
}
