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
                List<PendingProviderPaymentDTO>? pendingProviderPayments = null;
                List<ProviderWithdrawalDTO>? pendingWithdrawals = null;
                List<UnconfirmedBookingRefundDTO>? approvedRefunds = null;
                List<PendingProviderPaymentDTO>? approvedProviderPayments = null;
                List<ProviderWithdrawalDTO>? processedWithdrawals = null;

                // Load dashboard data
                if (tab == null || tab == "dashboard")
                {
                    dashboard = await _paymentService.GetDashboardAsync();
                }

                // Load unconfirmed bookings for refund (pending)
                if (tab == "refunds")
                {
                    unconfirmedBookings = await _paymentService.GetUnconfirmedBookingsForRefundAsync() ?? new List<UnconfirmedBookingRefundDTO>();
                }

                // Load approved refunds (completed)
                if (tab == "approved-refunds")
                {
                    approvedRefunds = await _paymentService.GetApprovedRefundsAsync() ?? new List<UnconfirmedBookingRefundDTO>();
                }

                // Load pending provider payments
                if (tab == "provider-payments")
                {
                    pendingProviderPayments = await _paymentService.GetPendingProviderPaymentsAsync() ?? new List<PendingProviderPaymentDTO>();
                }

                // Load approved provider payments (completed)
                if (tab == "approved-provider-payments")
                {
                    approvedProviderPayments = await _paymentService.GetApprovedProviderPaymentsAsync() ?? new List<PendingProviderPaymentDTO>();
                }

                // Load pending withdrawals
                if (tab == "withdrawals")
                {
                    pendingWithdrawals = await _paymentService.GetPendingWithdrawalsAsync() ?? new List<ProviderWithdrawalDTO>();
                }

                // Load processed withdrawals (completed/rejected)
                if (tab == "processed-withdrawals")
                {
                    processedWithdrawals = await _paymentService.GetProcessedWithdrawalsAsync() ?? new List<ProviderWithdrawalDTO>();
                }

                var model = new PaymentManagementViewModel
                {
                    Dashboard = dashboard,
                    UnconfirmedBookings = unconfirmedBookings ?? new List<UnconfirmedBookingRefundDTO>(),
                    PendingProviderPayments = pendingProviderPayments ?? new List<PendingProviderPaymentDTO>(),
                    PendingWithdrawals = pendingWithdrawals ?? new List<ProviderWithdrawalDTO>(),
                    ApprovedRefunds = approvedRefunds ?? new List<UnconfirmedBookingRefundDTO>(),
                    ApprovedProviderPayments = approvedProviderPayments ?? new List<PendingProviderPaymentDTO>(),
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
        public async Task<IActionResult> ApproveProviderPayment([FromBody] ApproveProviderPaymentRequestDTO request)
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
                var result = await _paymentService.ApproveProviderPaymentAsync(request.BookingId, request.AdminNote);
                return Json(new { success = result, message = result ? "Phê duyệt thanh toán thành công" : "Có lỗi xảy ra" });
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
        public List<PendingProviderPaymentDTO> PendingProviderPayments { get; set; } = new();
        public List<ProviderWithdrawalDTO> PendingWithdrawals { get; set; } = new();
        
        // Processed items (for viewing history)
        public List<UnconfirmedBookingRefundDTO> ApprovedRefunds { get; set; } = new();
        public List<PendingProviderPaymentDTO> ApprovedProviderPayments { get; set; } = new();
        public List<ProviderWithdrawalDTO> ProcessedWithdrawals { get; set; } = new();
    }
}

