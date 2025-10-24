# Tích hợp Thanh toán VNPay cho ASP.NET

## Tổng quan

Dự án đã được tích hợp hoàn chỉnh VNPay Payment Gateway theo hướng dẫn chính thức của VNPay. Hiện tại đang sử dụng **môi trường Sandbox** để test.

## Cấu hình

### 1. Thông tin VNPay Test Environment

File `appsettings.json` đã được cấu hình với thông tin sau:

```json
{
  "Vnpay": {
    "TmnCode": "NJJ0R8FS",
    "HashSecret": "BYKJBHPPZKQMKBIBGGXIYKWYFAYSJXCW",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "Command": "pay",
    "CurrCode": "VND",
    "Version": "2.1.0",
    "Locale": "vn",
    "PaymentBackReturnUrl": "http://localhost:5172/Customer/Payment/PaymentCallbackVnpay"
  },
  "TimeZoneId": "SE Asia Standard Time"
}
```

⚠️ **Lưu ý**: Khi deploy lên Production, cần thay đổi:
- `TmnCode` và `HashSecret` bằng thông tin thực từ VNPay
- `BaseUrl` thành `https://pay.vnpay.vn/vpcpay.html`
- `PaymentBackReturnUrl` thành URL thực của bạn

### 2. Đăng ký Service trong Program.cs

Service đã được đăng ký:

```csharp
// Connect VNPay API
builder.Services.AddScoped<IVnPayService, VnPayService>();
```

## Cấu trúc Code

### 1. Models (`Models/Payment/`)

- **PaymentInformationModel.cs**: Model chứa thông tin thanh toán
  - `OrderType`: Loại đơn hàng
  - `Amount`: Số tiền (double)
  - `OrderDescription`: Mô tả đơn hàng
  - `Name`: Tên người thanh toán

- **PaymentResponseModel.cs**: Model chứa kết quả từ VNPay
  - `Success`: Trạng thái thanh toán
  - `OrderId`, `TransactionId`, `PaymentId`: Các ID liên quan
  - `VnPayResponseCode`: Mã phản hồi từ VNPay
  - `Token`: Secure hash token

### 2. Services

#### VnPayLibrary (`Services/Customer/VnPayLibrary.cs`)

Thư viện chính xử lý VNPay API:
- `AddRequestData()`: Thêm dữ liệu request
- `AddResponseData()`: Thêm dữ liệu response
- `CreateRequestUrl()`: Tạo URL thanh toán với secure hash
- `ValidateSignature()`: Xác thực chữ ký từ VNPay
- `GetFullResponseData()`: Parse dữ liệu trả về từ VNPay
- `GetIpAddress()`: Lấy IP address của client

#### IVnPayService Interface (`Services/Customer/Interfaces/IVnPayService.cs`)

```csharp
public interface IVnPayService
{
    string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
    PaymentResponseModel PaymentExecute(IQueryCollection collections);
}
```

#### VnPayService Implementation (`Services/Customer/VnPayService.cs`)

Implement các method từ interface:
- `CreatePaymentUrl()`: Tạo URL chuyển hướng đến VNPay
- `PaymentExecute()`: Xử lý kết quả trả về từ VNPay

### 3. Controller

#### PaymentController (`Areas/Customer/Controllers/PaymentController.cs`)

Action methods chính:

```csharp
// Tạo URL thanh toán và redirect đến VNPay
public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)

// Callback từ VNPay sau khi thanh toán
[HttpGet]
public IActionResult PaymentCallbackVnpay()
```

### 4. Views

- **StartVnPay.cshtml**: Trang demo thanh toán VNPay với UI đẹp
- **Success.cshtml**: Trang hiển thị kết quả thanh toán thành công

## Cách sử dụng

### 1. Flow thanh toán cơ bản

```csharp
// Trong controller của bạn
var paymentInfo = new PaymentInformationModel
{
    OrderType = "billpayment",
    Amount = 387000, // Số tiền (VND)
    OrderDescription = "Thanh toán đơn hàng",
    Name = "Nguyen Van A"
};

return RedirectToAction("CreatePaymentUrlVnpay", "Payment", 
    new { area = "Customer", model = paymentInfo });
```

### 2. Flow đã tích hợp sẵn trong BookingServiceController

```csharp
case "VNPAY":
    return RedirectToAction(
        "StartVnPay", "Payment",
        new { area = "Customer", bookingIds = result.BookingIds, amount = amountStr });
```

### 3. Xử lý callback

Action `PaymentCallbackVnpay()` sẽ:
1. Nhận query parameters từ VNPay
2. Validate signature
3. Trả về JSON response với thông tin thanh toán

Response mẫu:
```json
{
  "success": true,
  "paymentMethod": "VnPay",
  "orderDescription": "Thanh toán đơn hàng",
  "orderId": "123456",
  "transactionId": "78910",
  "vnPayResponseCode": "00"
}
```

## Tài khoản Test VNPay

Sử dụng thông tin sau để test thanh toán:

- **Ngân hàng**: NCB
- **Số thẻ**: 9704198526191432198
- **Tên chủ thẻ**: NGUYEN VAN A
- **Ngày phát hành**: 07/15
- **Mật khẩu OTP**: 123456

## Mã phản hồi VNPay (vnp_ResponseCode)

- `00`: Giao dịch thành công
- `07`: Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường)
- `09`: Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng
- `10`: Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần
- `11`: Giao dịch không thành công do: Đã hết hạn chờ thanh toán
- `12`: Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa
- `13`: Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP)
- `24`: Giao dịch không thành công do: Khách hàng hủy giao dịch
- `51`: Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch
- `65`: Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày
- `75`: Ngân hàng thanh toán đang bảo trì
- `79`: Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định
- `99`: Các lỗi khác

## Production Deployment

Khi chuyển sang Production:

1. **Đăng ký tài khoản VNPay Production**
   - Liên hệ VNPay để ký hợp đồng
   - Nhận `TmnCode` và `HashSecret` thực

2. **Cập nhật appsettings.json**
   ```json
   {
     "Vnpay": {
       "TmnCode": "YOUR_PRODUCTION_TMN_CODE",
       "HashSecret": "YOUR_PRODUCTION_HASH_SECRET",
       "BaseUrl": "https://pay.vnpay.vn/vpcpay.html",
       "PaymentBackReturnUrl": "https://yourdomain.com/Customer/Payment/PaymentCallbackVnpay"
     }
   }
   ```

3. **Update appsettings.Production.json**
   - Đặt riêng config cho môi trường production
   - Không commit thông tin nhạy cảm lên git

4. **SSL Certificate**
   - VNPay yêu cầu HTTPS cho ReturnUrl
   - Đảm bảo certificate hợp lệ

## Tài liệu tham khảo

- [VNPay Sandbox Documentation](https://sandbox.vnpayment.vn/apis/docs/huong-dan-tich-hop/)
- [Danh sách ngân hàng test](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html#danh-sach-ngan-hang-ho-tro-thanh-toan-test)

## Troubleshooting

### Lỗi "Invalid Signature"
- Kiểm tra `HashSecret` trong config
- Đảm bảo không có khoảng trắng thừa
- Verify query string parameters được sắp xếp đúng thứ tự

### Lỗi "Timeout"
- Kiểm tra kết nối internet
- VNPay Sandbox có thể bảo trì, thử lại sau

### Callback không nhận được
- Kiểm tra `PaymentBackReturnUrl` trong config
- Đảm bảo URL có thể truy cập từ bên ngoài (không localhost khi deploy)
- Sử dụng ngrok cho local testing nếu cần

---

**Tích hợp hoàn tất!** 🎉

Mọi thắc mắc vui lòng tham khảo tài liệu VNPay hoặc liên hệ support.

