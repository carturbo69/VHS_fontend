using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models.Payment;
using VHS_frontend.Services.Customer;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Services.Customer.Interfaces;

[ApiController]
[Route("api/mobile/payment")]
public class MobilePaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly BookingServiceCustomer _bookingService;
    private readonly IConfiguration _config;

    public MobilePaymentController(
        IVnPayService vnPayService,
        BookingServiceCustomer bookingService,
        IConfiguration config)
    {
        _vnPayService = vnPayService;
        _bookingService = bookingService;
        _config = config;
    }

    // ============================================
    // 1) MOBILE: Tạo URL thanh toán
    // ============================================
    [HttpPost("create-url")]
    public IActionResult CreatePaymentUrl([FromBody] PaymentInformationModel model)
    {
        var callbackUrl =
            $"{Request.Scheme}://{Request.Host}/api/mobile/payment/vnpay-return";

        var url = _vnPayService.CreatePaymentUrl(model, HttpContext, callbackUrl);

        return Ok(new { paymentUrl = url });
    }

    // 2) MOBILE: VNPay callback 
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> MobileVnpayReturn(CancellationToken ct)
    {
        var response = _vnPayService.PaymentExecute(Request.Query);

        // Check VNPay transaction result
        if (!response.Success)
        {
            return BadRequest(new
            {
                success = false,
                code = response.VnPayResponseCode,
                message = "Thanh toán thất bại từ VNPay."
            });
        }

        // Parse Booking ID từ OrderInfo = "BOOKINGS:<guid>"
        var orderInfo = response.OrderDescription ?? "";
        if (!orderInfo.StartsWith("BOOKINGS:", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                success = false,
                message = "OrderInfo không hợp lệ."
            });
        }

        var bookingIdStr = orderInfo.Substring("BOOKINGS:".Length);

        if (!Guid.TryParse(bookingIdStr, out var bookingId))
        {
            return BadRequest(new
            {
                success = false,
                message = "BookingId không hợp lệ."
            });
        }

        // Thời điểm thanh toán
        var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var paymentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);

        //  CONFIRM PAYMENT VỚI BOOKING SERVICE 
      
        try
        {
            var dto = new ConfirmPaymentsDto
            {
                BookingIds = new List<Guid> { bookingId },
                PaymentMethod = "VNPAY",
                GatewayTxnId = $"VNPAY:{response.TransactionId}",
                CartItemIdsForCleanup = null,
                PaymentTime = paymentTime
            };


            await _bookingService.ConfirmPaymentsAsync(dto, ct);

            return Ok(new
            {
                success = true,
                bookingId,
                transactionId = response.TransactionId,
                message = "Thanh toán thành công và đã được xác nhận!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Xác nhận thanh toán thất bại.",
                error = ex.Message
            });
        }
    }
}
