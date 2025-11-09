using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public async Task<IActionResult> Approve(string id, string? returnUrl)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!Guid.TryParse(id, out var gid)) return RedirectToAction("Index");
            
            // Kiểm tra service có Status gì để gọi đúng endpoint
            var service = await _service.GetServiceStatusAsync(gid, token);
            if (service == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction("Index");
            }
            
            bool ok;
            if (service.Status == "PendingUpdate")
            {
                // Duyệt chỉnh sửa
                ok = await _service.ApproveUpdateAsync(gid, token);
                TempData[ok ? "Success" : "Error"] = ok ? "Đã duyệt chỉnh sửa dịch vụ." : "Duyệt chỉnh sửa thất bại.";
            }
            else if (service.Status == "Pending")
            {
                // Duyệt dịch vụ mới
                ok = await _service.ApproveAsync(gid, token);
                TempData[ok ? "Success" : "Error"] = ok ? "Đã duyệt dịch vụ." : "Duyệt thất bại.";
            }
            else
            {
                TempData["Error"] = $"Không thể duyệt dịch vụ với trạng thái '{service.Status}'.";
                return RedirectToAction("Index");
            }
            
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string id, string? reason, string? returnUrl)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!Guid.TryParse(id, out var gid)) return RedirectToAction("Index");
            
            // Kiểm tra service có Status gì để gọi đúng endpoint
            var service = await _service.GetServiceStatusAsync(gid, token);
            if (service == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction("Index");
            }
            
            bool ok;
            if (service.Status == "PendingUpdate")
            {
                // Từ chối chỉnh sửa
                ok = await _service.RejectUpdateAsync(gid, reason, token);
                TempData[ok ? "Success" : "Error"] = ok ? "Đã từ chối chỉnh sửa dịch vụ." : "Từ chối chỉnh sửa thất bại.";
            }
            else if (service.Status == "Pending")
            {
                // Từ chối dịch vụ mới
                ok = await _service.RejectAsync(gid, reason, token);
                TempData[ok ? "Success" : "Error"] = ok ? "Đã từ chối dịch vụ." : "Từ chối thất bại.";
            }
            else
            {
                TempData["Error"] = $"Không thể từ chối dịch vụ với trạng thái '{service.Status}'.";
                return RedirectToAction("Index");
            }
            
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!Guid.TryParse(id, out var gid)) return RedirectToAction("Index");
            var item = await _service.GetDetailAsync(gid, token);
            if (item == null) { TempData["Error"] = "Không tải được chi tiết dịch vụ."; return RedirectToAction("Index"); }
            ViewData["Title"] = "Chi tiết dịch vụ";
            // Lấy returnUrl từ query string
            ViewBag.ReturnUrl = Request.Query["returnUrl"].ToString();
            // Truyền backend base URL để view sử dụng (lấy từ HttpContext.RequestServices)
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            ViewBag.BackendBase = configuration["Apis:Backend"] ?? "http://localhost:5154";
            return View(item);
        }
    }
}


