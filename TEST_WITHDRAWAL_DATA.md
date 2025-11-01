# Dữ liệu Test Form Rút Tiền Provider

## 📋 Các trường hợp test

### ✅ Test Case 1: Dữ liệu hợp lệ (Thành công)

**Trường hợp A: Rút tiền tối thiểu**
- Số tiền: `100000`
- Số tài khoản: `1234567890`
- Tên ngân hàng: `Vietcombank`
- QR Code: (để trống)
- Ghi chú: (để trống)

**Trường hợp B: Rút tiền với QR code**
- Số tiền: `500000`
- Số tài khoản: `9876543210`
- Tên ngân hàng: `Techcombank`
- QR Code: `https://example.com/qrcode.png`
- Ghi chú: `Cần rút gấp để thanh toán`

**Trường hợp C: Rút tiền với ghi chú**
- Số tiền: `250000`
- Số tài khoản: `5555555555`
- Tên ngân hàng: `MB bank`
- QR Code: (để trống)
- Ghi chú: `Rút tiền tháng 10`

**Trường hợp D: Rút tối đa (nếu có đủ số dư)**
- Số tiền: (Nhập số dư có thể rút hiển thị trên form)
- Số tài khoản: `1111111111`
- Tên ngân hàng: `MB bank`
- QR Code: (để trống)
- Ghi chú: (để trống)

---

### ❌ Test Case 2: Validation Errors

**TC2.1: Số tiền = 0**
- Số tiền: `0`
- Số tài khoản: `1234567890`
- Tên ngân hàng: `Vietcombank`
- **Kỳ vọng**: Alert "Số tiền phải là số và lớn hơn 0"

**TC2.2: Số tiền < 0**
- Số tiền: `-10000`
- Số tài khoản: `1234567890`
- Tên ngân hàng: `Vietcombank`
- **Kỳ vọng**: Alert "Số tiền phải là số và lớn hơn 0"

**TC2.3: Số tiền > số dư khả dụng**
- Số tiền: (Nhập số lớn hơn số dư hiển thị)
- Số tài khoản: `1234567890`
- Tên ngân hàng: `Vietcombank`
- **Kỳ vọng**: Alert "Số tiền không được vượt quá số dư có sẵn: X VNĐ"

**TC2.4: Số tiền = rỗng/null**
- Số tiền: (để trống)
- Số tài khoản: `1234567890`
- Tên ngân hàng: `Vietcombank`
- **Kỳ vọng**: Alert "Vui lòng nhập số tiền"

**TC2.5: Số tài khoản = rỗng**
- Số tiền: `100000`
- Số tài khoản: (để trống)
- Tên ngân hàng: `Vietcombank`
- **Kỳ vọng**: Alert "Vui lòng nhập số tài khoản ngân hàng"

**TC2.6: Tên ngân hàng = rỗng**
- Số tiền: `100000`
- Số tài khoản: `1234567890`
- Tên ngân hàng: (để trống)
- **Kỳ vọng**: Alert "Vui lòng nhập tên ngân hàng"

**TC2.7: Tất cả trường rỗng**
- Số tiền: (để trống)
- Số tài khoản: (để trống)
- Tên ngân hàng: (để trống)
- **Kỳ vọng**: Alert lỗi đầu tiên tìm thấy

---

## 🧪 Hướng dẫn test bằng Browser Console

Mở Browser Console (F12) và chạy các lệnh sau:

### Test gửi request hợp lệ:
```javascript
// Copy và paste vào Console
const testData = {
    Amount: 100000,
    BankAccount: "1234567890",
    BankName: "Vietcombank",
    QrCode: null,
    Note: null
};

fetch('/Provider/ProviderWithdrawal/RequestWithdrawal', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
    },
    body: JSON.stringify(testData)
})
.then(res => res.json())
.then(data => {
    console.log('Response:', data);
    if (data.success) {
        alert('✅ Thành công: ' + data.message);
    } else {
        alert('❌ Lỗi: ' + data.message);
    }
})
.catch(err => console.error('Error:', err));
```

### Test validation:
```javascript
// Test với Amount = 0
const invalidData = {
    Amount: 0,
    BankAccount: "1234567890",
    BankName: "Vietcombank"
};

fetch('/Provider/ProviderWithdrawal/RequestWithdrawal', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
    },
    body: JSON.stringify(invalidData)
})
.then(res => res.json())
.then(data => console.log('Response:', data))
.catch(err => console.error('Error:', err));
```

---

## 📝 Checklist Test

- [ ] Test rút tiền thành công với dữ liệu hợp lệ
- [ ] Test validation số tiền <= 0
- [ ] Test validation số tiền > số dư
- [ ] Test validation số tài khoản rỗng
- [ ] Test validation tên ngân hàng rỗng
- [ ] Test rút tiền với QR code
- [ ] Test rút tiền với ghi chú
- [ ] Kiểm tra console.log để xem dữ liệu được gửi
- [ ] Kiểm tra backend log để xem dữ liệu nhận được

---

## 🔍 Kiểm tra Logs

### Frontend Logs (Browser Console):
- `Data gửi BE:` - Xem dữ liệu JavaScript gửi đi
- `BE trả về:` - Xem response từ backend

### Backend Logs (Terminal/Console):
- `[DEBUG] Provider rút tiền - ProviderId: ...`
- `[DEBUG] Withdraw request: Amount=..., BankAccount='...', ...`
- `[DEBUG] Validation errors: ...` (nếu có lỗi)
- `[DEBUG] GrossCompletedAmount: ...`
- `[DEBUG] Đã rút: ..., Đang chờ rút: ..., Số dư có thể rút: ...`

