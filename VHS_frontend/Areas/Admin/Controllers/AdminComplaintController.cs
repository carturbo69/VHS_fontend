using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Complaint;
using VHS_frontend.Services.Admin;
using System.Linq;

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
                
                // Debug logging
                Console.WriteLine($"[AdminComplaintController] Filter received - Status: '{filter.Status}', Type: '{filter.Type}', Search: '{filter.Search}', Page: {filter.Page}, PageSize: {filter.PageSize}");
                
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
                    
                    // Loại bỏ các khiếu nại liên quan đến đơn tự động hủy
                    var originalCount = result.Complaints.Count;
                    result.Complaints = result.Complaints.Where(c => !IsAutoCancelComplaint(c)).ToList();
                    
                    // Cập nhật TotalCount sau khi filter
                    result.TotalCount = result.Complaints.Count;
                    
                    if (originalCount != result.Complaints.Count)
                    {
                        Console.WriteLine($"[AdminComplaintController] Filtered out {originalCount - result.Complaints.Count} auto-cancel complaints");
                    }
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
                
                if (result != null && result.Success)
                {
                    // Kiểm tra xem có RefundRequest được tạo không
                    bool refundRequestCreated = false;
                    if (result.Data != null)
                    {
                        // Sử dụng System.Text.Json để parse Data object
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(result.Data);
                        using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
                        if (doc.RootElement.TryGetProperty("RefundRequestCreated", out var refundCreatedProp) && 
                            refundCreatedProp.ValueKind == System.Text.Json.JsonValueKind.True)
                        {
                            refundRequestCreated = true;
                        }
                    }
                    
                    if (refundRequestCreated && dto.Status == "Resolved")
                    {
                        TempData["Success"] = "Xử lý khiếu nại thành công! Yêu cầu hoàn tiền đã được tạo và đang chờ duyệt trong mục Quản lý Thanh toán.";
                    }
                    else
                    {
                        TempData["Success"] = result.Message ?? "Xử lý khiếu nại thành công!";
                    }
                }
                else
                {
                    TempData["Error"] = result?.Message ?? "Không thể xử lý khiếu nại!";
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

        /// <summary>
        /// Kiểm tra xem khiếu nại có liên quan đến đơn tự động hủy không
        /// </summary>
        private bool IsAutoCancelComplaint(AdminComplaintDTO complaint)
        {
            if (complaint == null) return false;
            
            // Kiểm tra description có chứa từ khóa về auto cancel
            if (!string.IsNullOrWhiteSpace(complaint.Description))
            {
                var descLower = complaint.Description.ToLower();
                var autoCancelKeywords = new[]
                {
                    "tự động hủy",
                    "auto cancel",
                    "auto-cancel",
                    "automatic cancel",
                    "automatic cancellation",
                    "hệ thống tự động hủy",
                    "đơn tự động hủy",
                    "booking tự động hủy",
                    "hết thời gian chờ xác nhận",
                    "đã hết thời gian chờ xác nhận",
                    "không kịp xác nhận",
                    "nhà cung cấp không kịp xác nhận",
                    "bạn không kịp xác nhận",
                    "trong thời gian quy định",
                    "hết thời gian",
                    "đã hết thời gian",
                    "nhà cung cấp không kịp",
                    "được hủy tự động"
                };
                
                if (autoCancelKeywords.Any(keyword => descLower.Contains(keyword)))
                {
                    return true;
                }
            }
            
            // Kiểm tra title có chứa từ khóa về auto cancel
            if (!string.IsNullOrWhiteSpace(complaint.Title))
            {
                var titleLower = complaint.Title.ToLower();
                var autoCancelKeywords = new[]
                {
                    "tự động hủy",
                    "auto cancel",
                    "auto-cancel",
                    "đơn hàng bị từ chối",
                    "bị từ chối",
                    "hết thời gian"
                };
                
                if (autoCancelKeywords.Any(keyword => titleLower.Contains(keyword)))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}








