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

            // Làm tròn số tiền về số nguyên (VND không có phần thập phân)
            var amountVnd = Math.Round(model.Amount, 0, MidpointRounding.AwayFromZero);

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
            // Chỉ gửi OrderDescription (đã chứa "BOOKINGS:guid1,guid2"), không thêm name và amount
            pay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
            pay.AddRequestData("vnp_OrderType", model.OrderType);
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack ?? "");
            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl =
                pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"] ?? "", _configuration["Vnpay:HashSecret"] ?? "");

            return paymentUrl;
        }
        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string overrideReturnUrl)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(
                _configuration["TimeZoneId"] ?? "SE Asia Standard Time"
            );

            var tick = DateTime.Now.Ticks.ToString();

            // Amount
            var amountVnd = Math.Round(model.Amount, 0, MidpointRounding.AwayFromZero);
            var amountInXu = (long)amountVnd * 100;

            // Time & IP
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);

            var pay = new VnPayLibrary();
            var ip = pay.GetIpAddress(context);

            //Request Data

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"] ?? "2.1.0");
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"] ?? "pay");
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"] ?? "");

            pay.AddRequestData("vnp_Amount", amountInXu.ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"] ?? "VND");

            pay.AddRequestData("vnp_IpAddr", ip);
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"] ?? "vn");

            // OrderInfo 
            pay.AddRequestData("vnp_OrderInfo", model.OrderDescription);

            pay.AddRequestData("vnp_OrderType", model.OrderType);

            
            pay.AddRequestData("vnp_ReturnUrl", overrideReturnUrl);

            // Unique ref 
            pay.AddRequestData("vnp_TxnRef", tick);

            // ===== Tạo URL =====
            var paymentUrl = pay.CreateRequestUrl(
                _configuration["Vnpay:BaseUrl"] ?? "",
                _configuration["Vnpay:HashSecret"] ?? ""
            );

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
