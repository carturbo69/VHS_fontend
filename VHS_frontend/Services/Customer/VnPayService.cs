using VHS_frontend.Models.Payment;
using VHS_frontend.Services.Customer.Interfaces;

namespace VHS_frontend.Services.Customer
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"] ?? "SE Asia Standard Time");
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VnPayLibrary();
            var urlCallBack = _configuration["Vnpay:PaymentBackReturnUrl"];

            // ✅ Validate và tính toán số tiền đúng cách
            // VNPay yêu cầu số tiền từ 5,000 đến dưới 1 tỷ đồng
            const double MIN_AMOUNT = 5000.0; // 5,000 VND
            const double MAX_AMOUNT = 999999999.0; // 999,999,999 VND (dưới 1 tỷ)

            // Làm tròn số tiền về số nguyên (VND không có phần thập phân)
            var amountVnd = Math.Round(model.Amount, 0, MidpointRounding.AwayFromZero);
            
            // Validate số tiền
            if (amountVnd < MIN_AMOUNT)
            {
                throw new ArgumentException($"Số tiền thanh toán ({amountVnd:N0} VND) quá nhỏ. Số tiền tối thiểu là {MIN_AMOUNT:N0} VND.");
            }
            
            if (amountVnd >= MAX_AMOUNT + 1)
            {
                throw new ArgumentException($"Số tiền thanh toán ({amountVnd:N0} VND) quá lớn. Số tiền tối đa là {MAX_AMOUNT:N0} VND.");
            }

            // Chuyển đổi từ VND sang xu (VNPay yêu cầu số tiền theo xu)
            // Sử dụng long để tránh overflow và đảm bảo chính xác
            var amountInXu = (long)amountVnd * 100;

            // Debug log
            System.Diagnostics.Debug.WriteLine($"[VNPay] Amount (VND): {amountVnd:N0}, Amount (Xu): {amountInXu:N0}");

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"] ?? "2.1.0");
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"] ?? "pay");
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"] ?? "");
            pay.AddRequestData("vnp_Amount", amountInXu.ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"] ?? "VND");
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"] ?? "vn");
            // 🔑 Chỉ gửi OrderDescription (đã chứa "BOOKINGS:guid1,guid2"), không thêm name và amount
            pay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
            pay.AddRequestData("vnp_OrderType", model.OrderType);
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack ?? "");
            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl =
                pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"] ?? "", _configuration["Vnpay:HashSecret"] ?? "");

            return paymentUrl;
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"] ?? "");

            return response;
        }
    }
}
