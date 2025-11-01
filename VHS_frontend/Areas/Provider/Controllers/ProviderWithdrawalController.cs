using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Withdrawal;
using VHS_frontend.Services.Provider;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderWithdrawalController : Controller
    {
        private readonly ProviderWithdrawalService _withdrawalService;

        public ProviderWithdrawalController(ProviderWithdrawalService withdrawalService)
        {
            _withdrawalService = withdrawalService;
        }

        public async Task<IActionResult> Index()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "provider", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Provider";
            
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _withdrawalService.SetBearerToken(token);

            try
            {
                var balance = await _withdrawalService.GetBalanceAsync();
                var withdrawals = await _withdrawalService.GetWithdrawalsAsync();

                var model = new ProviderWithdrawalViewModel
                {
                    Balance = balance,
                    Withdrawals = withdrawals ?? new List<ProviderWithdrawalDTO>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi khi tải dữ liệu: {ex.Message}";
                return View(new ProviderWithdrawalViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> RequestWithdrawal([FromBody] ProviderWithdrawalRequestDTO request)
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(accountId) || !string.Equals(role, "provider", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = string.Join(", ", errors) });
            }
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _withdrawalService.SetBearerToken(token);
            try
            {
                var result = await _withdrawalService.RequestWithdrawalAsync(request);
                return Json(new { success = result, message = result ? "Yêu cầu rút tiền đã được gửi thành công" : "Có lỗi xảy ra khi gửi yêu cầu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class ProviderWithdrawalViewModel
    {
        public ProviderBalanceDTO? Balance { get; set; }
        public List<ProviderWithdrawalDTO> Withdrawals { get; set; } = new();
    }
}


