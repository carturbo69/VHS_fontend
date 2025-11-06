using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AddressController : Controller
    {
        private readonly UserAddressService _userAddressService;

        public AddressController(UserAddressService userAddressService)
        {
            _userAddressService = userAddressService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(
            [FromForm] Guid? AddressId, 
            [FromForm] string streetAddress, 
            [FromForm] string wardName, 
            [FromForm] string provinceName, 
            [FromForm] double? Latitude, 
            [FromForm] double? Longitude, 
            [FromForm] string districtName = "", 
            [FromForm] string? recipientName = null, 
            [FromForm] string? recipientPhone = null)
        {
            Console.WriteLine("=== AddressController.Upsert called ===");
            Console.WriteLine($"Parameters: AddressId={AddressId}, streetAddress={streetAddress}, wardName={wardName}, provinceName={provinceName}, districtName={districtName}");
            Console.WriteLine($"RecipientName={recipientName}, RecipientPhone={recipientPhone}");
            
            var jwt = HttpContext.Session.GetString("JWToken");
            Console.WriteLine($"JWT exists: {!string.IsNullOrWhiteSpace(jwt)}");
            
            if (string.IsNullOrWhiteSpace(jwt))
            {
                Console.WriteLine("ERROR: No JWT token");
                TempData["ToastError"] = "Bạn cần đăng nhập";
                return RedirectToAction("Index", "BookingService");
            }

            // Validate required fields (districtName là optional)
            if (string.IsNullOrWhiteSpace(streetAddress) || 
                string.IsNullOrWhiteSpace(wardName) || 
                string.IsNullOrWhiteSpace(provinceName))
            {
                TempData["ToastError"] = "Vui lòng điền đầy đủ thông tin địa chỉ (Địa chỉ, Phường/Xã, Tỉnh/Thành)";
                return RedirectToAction("Index", "BookingService");
            }

            try
            {
                if (AddressId.HasValue && AddressId != Guid.Empty)
                {
                    Console.WriteLine("Updating address...");
                    // Convert empty string to null để tránh validation issues
                    var finalDistrictName = string.IsNullOrWhiteSpace(districtName) ? null : districtName;
                    var finalRecipientName = string.IsNullOrWhiteSpace(recipientName) ? null : recipientName;
                    var finalRecipientPhone = string.IsNullOrWhiteSpace(recipientPhone) ? null : recipientPhone;
                    
                    Console.WriteLine($"Final values - DistrictName: '{finalDistrictName}', RecipientName: '{finalRecipientName}', RecipientPhone: '{finalRecipientPhone}'");
                    
                    // Cập nhật địa chỉ
                    var result = await _userAddressService.UpdateAddressAsync(
                        addressId: AddressId.Value,
                        provinceName: provinceName,
                        districtName: finalDistrictName ?? "",
                        wardName: wardName,
                        streetAddress: streetAddress,
                        latitude: Latitude,
                        longitude: Longitude,
                        recipientName: finalRecipientName,
                        recipientPhone: finalRecipientPhone,
                        jwt: jwt
                    );

                    Console.WriteLine($"Update result: Success={result.Success}, Message={result.Message}");
                    
                    if (result.Success)
                    {
                        TempData["ToastSuccess"] = result.Message ?? "Cập nhật địa chỉ thành công!";
                    }
                    else
                    {
                        TempData["ToastError"] = result.Message ?? "Không thể cập nhật địa chỉ";
                    }
                }
                else
                {
                    Console.WriteLine("Creating new address...");
                    // Tạo địa chỉ mới
                    var result = await _userAddressService.CreateAddressAsync(
                        provinceName: provinceName,
                        districtName: districtName,
                        wardName: wardName,
                        streetAddress: streetAddress,
                        latitude: Latitude,
                        longitude: Longitude,
                        recipientName: recipientName,
                        recipientPhone: recipientPhone,
                        jwt: jwt
                    );

                    Console.WriteLine($"Service result: Success={result.Success}, Message={result.Message}");
                    
                    if (result.Success)
                    {
                        TempData["ToastSuccess"] = result.Message ?? "Thêm địa chỉ thành công!";
                    }
                    else
                    {
                        TempData["ToastError"] = result.Message ?? "Không thể thêm địa chỉ";
                    }
                }

                return RedirectToAction("Index", "BookingService");
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "BookingService");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var jwt = HttpContext.Session.GetString("JWToken");
            
            if (string.IsNullOrWhiteSpace(jwt))
            {
                TempData["ToastError"] = "Bạn cần đăng nhập";
                return RedirectToAction("Index", "BookingService");
            }

            try
            {
                var result = await _userAddressService.DeleteAddressAsync(id, jwt);

                if (result.Success)
                {
                    TempData["ToastSuccess"] = result.Message ?? "Xóa địa chỉ thành công!";
                }
                else
                {
                    TempData["ToastError"] = result.Message ?? "Không thể xóa địa chỉ";
                }

                return RedirectToAction("Index", "BookingService");
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "BookingService");
            }
        }
    }
}

