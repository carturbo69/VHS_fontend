using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Complaint;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminComplaintController : Controller
    {
        private readonly AdminComplaintService _complaintService;

        public AdminComplaintController(AdminComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        public async Task<IActionResult> Index([FromQuery] AdminComplaintFilterDTO filter)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _complaintService.SetBearerToken(token);
            
            try
            {
                // Set default values if not provided
                if (filter == null)
                {
                    filter = new AdminComplaintFilterDTO();
                }
                if (filter.Page <= 0) filter.Page = 1;
                if (filter.PageSize <= 0) filter.PageSize = 10;
                
                var result = await _complaintService.GetAllAsync(filter);
                
                if (result == null)
                {
                    Console.WriteLine($"[AdminComplaintController] Service returned null, creating empty result");
                    result = new PaginatedAdminComplaintDTO
                    {
                        Complaints = new List<AdminComplaintDTO>(),
                        TotalCount = 0,
                        Page = filter.Page,
                        PageSize = filter.PageSize
                    };
                }
                else
                {
                    Console.WriteLine($"[AdminComplaintController] Service returned {result.Complaints.Count} complaints, TotalCount: {result.TotalCount}");
                }

                ViewBag.Filter = filter;
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải danh sách khiếu nại: " + ex.Message;
                Console.WriteLine($"[AdminComplaintController] Error: {ex.Message}");
                Console.WriteLine($"[AdminComplaintController] StackTrace: {ex.StackTrace}");
                return View(new PaginatedAdminComplaintDTO { Complaints = new List<AdminComplaintDTO>() });
            }
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _complaintService.SetBearerToken(token);
            
            try
            {
                var complaint = await _complaintService.GetDetailsAsync(id);
                
                if (complaint == null)
                {
                    TempData["Error"] = "Không tìm thấy khiếu nại";
                    return RedirectToAction("Index");
                }

                return View(complaint);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải chi tiết khiếu nại: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> HandleComplaint(Guid id, [FromForm] HandleComplaintDTO dto)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(token))
                    _complaintService.SetBearerToken(token);

                var result = await _complaintService.HandleComplaintAsync(id, dto);
                
                if (result != null)
                {
                    TempData["Success"] = "Xử lý khiếu nại thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể xử lý khiếu nại!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xử lý khiếu nại: " + ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(token))
                    _complaintService.SetBearerToken(token);

                var stats = await _complaintService.GetStatisticsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}








