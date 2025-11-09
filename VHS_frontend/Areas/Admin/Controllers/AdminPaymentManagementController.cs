using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Payment;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminPaymentManagementController : Controller
    {
        private readonly PaymentManagementService _paymentService;

        public AdminPaymentManagementController(PaymentManagementService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<IActionResult> Index(string? tab = null)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            ViewBag.Tab = tab ?? "dashboard";
            
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _paymentService.SetBearerToken(token);

            try
            {
                PaymentDashboardDTO? dashboard = null;
                List<UnconfirmedBookingRefundDTO>? unconfirmedBookings = null;
                List<ProviderWithdrawalDTO>? pendingWithdrawals = null;
                List<UnconfirmedBookingRefundDTO>? approvedRefunds = null;
                List<ProviderWithdrawalDTO>? processedWithdrawals = null;

                // Always load dashboard data to show notification counts on tabs
                dashboard = await _paymentService.GetDashboardAsync();

                // Load all data needed for notification counts (load in parallel for better performance)
                // Load unconfirmed bookings for refund (pending) - always load for notification count
                var unconfirmedBookingsTask = _paymentService.GetUnconfirmedBookingsForRefundAsync();

                // Load approved refunds - always load for notification count
                var approvedRefundsTask = _paymentService.GetApprovedRefundsAsync();

                // Load pending withdrawals - always load for notification count
                var pendingWithdrawalsTask = _paymentService.GetPendingWithdrawalsAsync();

                // Load processed withdrawals - always load for notification count
                var processedWithdrawalsTask = _paymentService.GetProcessedWithdrawalsAsync();

                // Wait for all tasks to complete and get results
                unconfirmedBookings = await unconfirmedBookingsTask ?? new List<UnconfirmedBookingRefundDTO>();
                approvedRefunds = await approvedRefundsTask ?? new List<UnconfirmedBookingRefundDTO>();
                pendingWithdrawals = await pendingWithdrawalsTask ?? new List<ProviderWithdrawalDTO>();
                processedWithdrawals = await processedWithdrawalsTask ?? new List<ProviderWithdrawalDTO>();

                var model = new PaymentManagementViewModel
                {
                    Dashboard = dashboard,
                    UnconfirmedBookings = unconfirmedBookings ?? new List<UnconfirmedBookingRefundDTO>(),
                    PendingWithdrawals = pendingWithdrawals ?? new List<ProviderWithdrawalDTO>(),
                    ApprovedRefunds = approvedRefunds ?? new List<UnconfirmedBookingRefundDTO>(),
                    ProcessedWithdrawals = processedWithdrawals ?? new List<ProviderWithdrawalDTO>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi khi tải dữ liệu: {ex.Message}";
                return View(new PaymentManagementViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRefund([FromBody] ApproveRefundRequestDTO request)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _paymentService.SetBearerToken(token);

            try
            {
                var result = await _paymentService.ApproveRefundForUnconfirmedBookingAsync(request.BookingId, request.AdminNote);
                return Json(new { success = result, message = result ? "Hoàn tiền thành công" : "Có lỗi xảy ra" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectRefund([FromBody] ApproveRefundRequestDTO request)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _paymentService.SetBearerToken(token);

            try
            {
                var result = await _paymentService.RejectRefundForUnconfirmedBookingAsync(request.BookingId, request.AdminNote);
                return Json(new { success = result, message = result ? "Từ chối hoàn tiền thành công" : "Có lỗi xảy ra" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ApproveWithdrawal([FromBody] ApproveWithdrawalRequestDTO request)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _paymentService.SetBearerToken(token);

            try
            {
                var result = await _paymentService.ApproveWithdrawalAsync(request.WithdrawalId, request.Action, request.AdminNote);
                return Json(new { success = result, message = result ? (request.Action == "Approve" ? "Phê duyệt rút tiền thành công" : "Từ chối rút tiền thành công") : "Có lỗi xảy ra" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class PaymentManagementViewModel
    {
        public PaymentDashboardDTO? Dashboard { get; set; }
        
        // Pending items
        public List<UnconfirmedBookingRefundDTO> UnconfirmedBookings { get; set; } = new();
        public List<ProviderWithdrawalDTO> PendingWithdrawals { get; set; } = new();
        
        // Processed items (for viewing history)
        public List<UnconfirmedBookingRefundDTO> ApprovedRefunds { get; set; } = new();
        public List<ProviderWithdrawalDTO> ProcessedWithdrawals { get; set; } = new();
    }
}

