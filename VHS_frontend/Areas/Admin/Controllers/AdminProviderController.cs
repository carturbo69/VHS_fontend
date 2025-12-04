using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Provider;
using VHS_frontend.Services.Admin;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminProviderController : Controller
    {
        private readonly ProviderAdminService _svc;
        private readonly ServiceManagementService _serviceManagementService;
        private readonly ProviderProfileService _providerProfileService;
        private readonly AdminBookingService _adminBookingService;

        public AdminProviderController(
            ProviderAdminService svc,
            ServiceManagementService serviceManagementService,
            ProviderProfileService providerProfileService,
            AdminBookingService adminBookingService)
        {
            _svc = svc;
            _serviceManagementService = serviceManagementService;
            _providerProfileService = providerProfileService;
            _adminBookingService = adminBookingService;
        }
        
        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _svc.SetBearerToken(token);
            }
        }
        
        private string? GetToken()
        {
            return HttpContext.Session.GetString("JWToken");
        }

        // GET: /Admin/AdminProvider?includeDeleted=false
        [HttpGet]
        public async Task<IActionResult> Index(bool includeDeleted = false, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var list = await _svc.GetAllAsync(includeDeleted, ct);
            ViewData["IncludeDeleted"] = includeDeleted;
            return View(list); // View: Areas/Admin/Views/AdminProvider/Index.cshtml (model: List<ProviderDTO>)
        }

        // GET: /Admin/AdminProvider/Get?id={guid}
        // Trả JSON phục vụ modal "Xem"
        [HttpGet]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var dto = await _svc.GetByIdAsync(id, ct);
            if (dto == null) return NotFound("Không tìm thấy Provider.");
            return Json(dto);
        }

        // DELETE: /Admin/AdminProvider/Delete?id={guid}
        // Soft-delete (ẩn) và tự động tạm dừng tất cả dịch vụ
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, [FromBody] Dictionary<string, string>? body = null, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var token = GetToken();
            
            // Lấy lý do từ body
            var lockReason = body?.GetValueOrDefault("LockReason");
            
            // Kiểm tra provider có đơn đang xử lý không
            try
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Lấy ProviderId từ AccountId
                    var providerIdStr = await _providerProfileService.GetProviderIdByAccountAsync(id.ToString(), token, ct);
                    
                    if (!string.IsNullOrWhiteSpace(providerIdStr) && Guid.TryParse(providerIdStr, out var providerId))
                    {
                        // Set token cho AdminBookingService
                        _adminBookingService.SetBearerToken(token);
                        
                        // Kiểm tra có đơn đang xử lý không
                        var hasActiveBookings = await _adminBookingService.CheckProviderHasActiveBookingsAsync(providerId, ct);
                        
                        if (hasActiveBookings)
                        {
                            return BadRequest("Không thể khóa tài khoản. Provider này đang có đơn hàng đang xử lý (Chờ xử lý, Đã xác nhận, hoặc Đang làm việc). Vui lòng đợi các đơn hàng hoàn thành hoặc hủy trước khi khóa.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu có lỗi khi kiểm tra, trả về lỗi để an toàn (không cho khóa nếu không chắc chắn)
                return BadRequest($"Không thể kiểm tra trạng thái đơn hàng: {ex.Message}. Vui lòng thử lại sau.");
            }
            
            // Xóa (khóa) tài khoản
            var res = await _svc.DeleteAsync(id, lockReason, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await SafeReadAsync(res, ct) ?? "Xoá thất bại.";
                return BadRequest(msg);
            }
            
            // Tự động tạm dừng tất cả dịch vụ của provider này
            try
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Lấy ProviderId từ AccountId
                    var providerId = await _providerProfileService.GetProviderIdByAccountAsync(id.ToString(), token, ct);
                    
                    if (!string.IsNullOrWhiteSpace(providerId))
                    {
                        // Lấy tất cả dịch vụ của provider
                        var services = await _serviceManagementService.GetServicesByProviderAsync(providerId, token, ct);
                        
                        if (services != null && services.Any())
                        {
                            // Tạm dừng tất cả dịch vụ có status = "Active"
                            foreach (var service in services.Where(s => 
                                string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase)))
                            {
                                try
                                {
                                    await _serviceManagementService.UpdateStatusAsync(
                                        service.ServiceId.ToString(), 
                                        "Paused", 
                                        token, 
                                        ct);
                                }
                                catch
                                {
                                    // Log lỗi nhưng không dừng quá trình
                                    // Có thể ghi log ở đây nếu cần
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Nếu có lỗi khi tạm dừng dịch vụ, vẫn trả về thành công cho việc khóa tài khoản
                // Có thể ghi log ở đây nếu cần
            }
            
            return NoContent();
        }

        // POST: /Admin/AdminProvider/Restore?id={guid}
        // Khôi phục tài khoản và tự động kích hoạt lại các dịch vụ bị tạm dừng (chỉ status = "Paused")
        [HttpPost]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var token = GetToken();
            
            // Khôi phục tài khoản
            var res = await _svc.RestoreAsync(id, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await SafeReadAsync(res, ct) ?? "Khôi phục thất bại.";
                return BadRequest(msg);
            }
            
            // Tự động kích hoạt lại các dịch vụ bị tạm dừng (chỉ status = "Paused")
            // KHÔNG động vào các dịch vụ do provider tự khóa (status khác)
            try
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    // Lấy ProviderId từ AccountId
                    var providerId = await _providerProfileService.GetProviderIdByAccountAsync(id.ToString(), token, ct);
                    
                    if (!string.IsNullOrWhiteSpace(providerId))
                    {
                        // Lấy tất cả dịch vụ của provider
                        var services = await _serviceManagementService.GetServicesByProviderAsync(providerId, token, ct);
                        
                        if (services != null && services.Any())
                        {
                            // Chỉ kích hoạt lại các dịch vụ có status = "Paused" (tạm dừng do admin khóa)
                            // KHÔNG động vào các dịch vụ có status khác (do provider tự khóa)
                            foreach (var service in services.Where(s => 
                                string.Equals(s.Status, "Paused", StringComparison.OrdinalIgnoreCase)))
                            {
                                try
                                {
                                    await _serviceManagementService.UpdateStatusAsync(
                                        service.ServiceId.ToString(), 
                                        "Active", 
                                        token, 
                                        ct);
                                }
                                catch
                                {
                                    // Log lỗi nhưng không dừng quá trình
                                    // Có thể ghi log ở đây nếu cần
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Nếu có lỗi khi kích hoạt dịch vụ, vẫn trả về thành công cho việc khôi phục tài khoản
                // Có thể ghi log ở đây nếu cần
            }
            
            return NoContent();
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
