namespace VHS_frontend.Services.Customer
{
    public class VnPayService
    {
        private readonly string _tmnCode;   // cấu hình từ appsettings
        private readonly string _hashSecret;
        private readonly string _endpoint;  // "https://pay.vnpay.vn/vpcpay.html"

        public VnPayService(IConfiguration cfg)
        {
            _tmnCode = cfg["VNPay:TmnCode"]!;
            _hashSecret = cfg["VNPay:HashSecret"]!;
            _endpoint = cfg["VNPay:Endpoint"]!;
        }

        public string CreatePaymentUrl(Guid orderId, decimal amount, string orderInfo, string returnUrl)
        {
            var vnp = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _tmnCode,
                ["vnp_Amount"] = ((long)(amount * 100)).ToString(), // nhân 100 theo quy định
                ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = "0.0.0.0",
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = orderId.ToString("N")
            };

            var query = string.Join("&", vnp.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var signData = CreateHmac(_hashSecret, query);
            return $"{_endpoint}?{query}&vnp_SecureHash={signData}";
        }

        public (bool IsSuccess, string Message, Guid? OrderId) ValidateReturn(IQueryCollection query)
        {
            // Lấy và loại bỏ vnp_SecureHash / vnp_SecureHashType để tự tính lại
            var dict = query.Where(kv => kv.Key.StartsWith("vnp_"))
                            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            if (!dict.TryGetValue("vnp_SecureHash", out var secureHash))
                return (false, "Missing hash", null);

            dict.Remove("vnp_SecureHash");

            var data = string.Join("&", dict.OrderBy(k => k.Key)
                                            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var calc = CreateHmac(_hashSecret, data);
            if (!secureHash.Equals(calc, StringComparison.OrdinalIgnoreCase))
                return (false, "Invalid hash", null);

            var resp = dict.TryGetValue("vnp_ResponseCode", out var code) ? code : "";
            var ok = resp == "00";
            Guid? orderId = null;
            if (dict.TryGetValue("vnp_TxnRef", out var refStr) && Guid.TryParseExact(refStr, "N", out var g))
                orderId = g;

            return (ok, ok ? "OK" : $"Code={resp}", orderId);
        }

        private static string CreateHmac(string secret, string data)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
