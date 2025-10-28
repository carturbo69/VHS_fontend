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
        public async Task<IActionResult> Upsert(Guid? AddressId, string streetAddress, string wardName, 
            string provinceName, double? Latitude, double? Longitude, string districtName = "")
        {
            Console.WriteLine("=== AddressController.Upsert called ===");
            Console.WriteLine($"Parameters: AddressId={AddressId}, streetAddress={streetAddress}, wardName={wardName}, provinceName={provinceName}, districtName={districtName}");
            
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
                    // TODO: Update address - Backend chưa có API update
                    TempData["ToastError"] = "Tính năng sửa địa chỉ đang được phát triển";
                    return RedirectToAction("Index", "BookingService");
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

