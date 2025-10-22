using System.Text.Json;

namespace VHS_frontend.Services.Customer
{
    public class MoMoService
    {
        private readonly string _partnerCode;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _endpoint; // "https://test-payment.momo.vn/v2/gateway/api/create"

        public MoMoService(IConfiguration cfg)
        {
            _partnerCode = cfg["MoMo:PartnerCode"]!;
            _accessKey = cfg["MoMo:AccessKey"]!;
            _secretKey = cfg["MoMo:SecretKey"]!;
            _endpoint = cfg["MoMo:Endpoint"]!;
        }

        public async Task<(string payUrl, string requestId)> CreatePaymentUrlAsync(Guid orderId, decimal amount, string orderInfo, string returnUrl, string notifyUrl)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var orderIdStr = orderId.ToString("N");
            var amountStr = ((long)amount).ToString();

            // rawSign theo tài liệu MoMo
            var raw = $"accessKey={_accessKey}&amount={amountStr}&extraData=&ipnUrl={notifyUrl}&orderId={orderIdStr}&orderInfo={orderInfo}&partnerCode={_partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
            var signature = HmacSha256(_secretKey, raw);

            var payload = new
            {
                partnerCode = _partnerCode,
                requestId,
                amount = amountStr,
                orderId = orderIdStr,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                requestType = "captureWallet",
                extraData = "",
                lang = "vi",
                signature
            };

            using var client = new HttpClient();
            var resp = await client.PostAsJsonAsync(_endpoint, payload);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();

            var payUrl = json.GetProperty("payUrl").GetString();
            return (payUrl!, requestId);
        }

        public (bool IsSuccess, string Message, Guid? OrderId) ValidateReturn(IQueryCollection query)
        {
            // MoMo redirect trả query: resultCode, orderId, requestId, signature...
            var dict = query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            if (!dict.TryGetValue("signature", out var sig)) return (false, "Missing signature", null);

            var raw = $"accessKey={_accessKey}&amount={dict["amount"]}&extraData={dict.GetValueOrDefault("extraData", "")}&message={dict.GetValueOrDefault("message", "")}&orderId={dict["orderId"]}&orderInfo={dict["orderInfo"]}&orderType={dict.GetValueOrDefault("orderType", "")}&partnerCode={dict["partnerCode"]}&payType={dict.GetValueOrDefault("payType", "")}&requestId={dict["requestId"]}&responseTime={dict.GetValueOrDefault("responseTime", "")}&resultCode={dict["resultCode"]}&transId={dict.GetValueOrDefault("transId", "")}";
            var calc = HmacSha256(_secretKey, raw);
            if (!calc.Equals(sig, StringComparison.OrdinalIgnoreCase)) return (false, "Invalid signature", null);

            var ok = dict["resultCode"] == "0";
            Guid? orderId = Guid.TryParseExact(dict["orderId"], "N", out var g) ? g : null;
            return (ok, ok ? "OK" : $"Code={dict["resultCode"]}", orderId);
        }

        public (bool IsSuccess, string Message, Guid? OrderId) ValidateIpn(string body)
        {
            // body JSON: cần tính lại chữ ký y như tài liệu (rawSign IPN)
            var json = JsonDocument.Parse(body).RootElement;
            var resultCode = json.GetProperty("resultCode").GetInt32();
            var orderIdStr = json.GetProperty("orderId").GetString();

            // TODO: tái tạo rawSign theo IPN spec và so sánh chữ ký

            Guid? orderId = Guid.TryParseExact(orderIdStr, "N", out var g) ? g : null;
            var ok = resultCode == 0;
            return (ok, ok ? "OK" : $"Code={resultCode}", orderId);
        }

        private static string HmacSha256(string secret, string data)
        {
            using var h = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
