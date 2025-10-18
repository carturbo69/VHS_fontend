using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Customer.Models.Booking;
using VHS_frontend.Areas.Customer.Models.BookingServiceDTOs;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class BookingServiceController : Controller
    {
        private readonly CartServiceCustomer _cartService;

        public BookingServiceController(CartServiceCustomer cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Gọi API lấy danh sách voucher trong giỏ hàng (dành cho khách hàng).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCartVouchers()
        {
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                var vouchers = await _cartService.GetCartVouchersAsync(jwtToken);

                return Ok(vouchers);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy voucher", error = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult Index()
        {
            // Dùng dữ liệu mẫu để hiển thị như demo
            var model = BookingViewModel.Sample();
            return View(model); // View đặt ở: Areas/Customer/Views/BookingService/Index.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeAddress(Guid SelectedAddressId)
        {
            // TODO: Tải giỏ hàng/BookingViewModel hiện tại của người dùng (từ session/db)
            var vm = BookingViewModel.Sample(); // demo

            var found = vm.Addresses.FirstOrDefault(a => a.AddressId == SelectedAddressId);
            if (found != null)
            {
                vm.Address = found;
                vm.SelectedAddressId = found.AddressId;
                // TODO: Lưu lại vm vào session/db
            }

            // Redirect về trang checkout (Thanh toán)
            return RedirectToAction(nameof(Checkout));
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var vm = BookingViewModel.Sample(); // TODO: lấy từ session/db
            return View("ThanhToan", vm);       // hoặc View mặc định của bạn
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(Guid SelectedAddressId, string SelectedPaymentCode)
        {
            // TODO: xử lý đặt hàng sử dụng SelectedAddressId + SelectedPaymentCode
            return RedirectToAction("Success");
        }

        // GET: /Customer/Address/Edit/{id}
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            // TODO: load address từ DB
            var addr = BookingViewModel.Sample().Addresses.FirstOrDefault(a => a.AddressId == id);
            if (addr == null) return NotFound();
            return View(addr);
        }

        // POST: /Customer/Address/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, UserAddressDto model)
        {
            // TODO: update vào DB
            // Sau khi update thì redirect về trang thanh toán
            return RedirectToAction("Checkout", "Booking");
        }

        // POST: /Customer/Address/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            // TODO: xóa khỏi DB
            return RedirectToAction("Checkout", "Booking");
        }

      // Areas/Customer/Controllers/BookingServiceController.cs


[HttpGet]
        public IActionResult ListHistoryBooking()
        {
            // Tabs trạng thái theo đúng thứ tự trong ảnh
            var statuses = new[]
            {
        "Chờ xác nhận",
        "Xác Nhận",
        "Bắt Đầu Làm Việc",
        "Hoàn thành",
        "Đã hủy",
        "Báo Cáo/Hoàn tiền",
        "Tất cả"
    };

            var items = new List<BookingServiceItemDTO>
    {
        // Chờ xác nhận
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddHours(-6),
            Status = "Chờ xác nhận",
            Address = "12 Lý Thái Tổ, Q.10, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Intimate Ziaja Store",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Dung Dịch Vệ Sinh Intimate With Lactic Acid ZIAJA 200ml",
            ServicePrice = 209000, ServiceUnitType = "đ",
            ServiceImages = "/images/sample1.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Gói đóng gói an toàn", Description = "Bọc chống sốc + thùng carton", Price = 10000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Giao nhanh 2H",       Description = "Ưu tiên điều phối nhanh",     Price = 15000, UnitType = "đ" },
            }
        },
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddHours(-10),
            Status = "Chờ xác nhận",
            Address = "23 Hoàng Diệu, Q.4, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Green House",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Vệ sinh máy lạnh treo tường",
            ServicePrice = 150000, ServiceUnitType = "đ",
            ServiceImages = "/images/ac.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Vệ sinh dàn nóng", Description = "Bổ sung dàn nóng", Price = 50000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Bảo hành 7 ngày",  Description = "Quay lại xử lý",  Price = 20000, UnitType = "đ" },
            }
        },

        // Xác Nhận
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddHours(-5),
            Status = "Xác Nhận",
            Address = "456 Lê Duẩn, Q.1, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Xuân Vũ Audio",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Cáp thay thế tai nghe Moxpad X3 có mic",
            ServicePrice = 180000, ServiceUnitType = "đ",
            ServiceImages = "/images/sample2.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Bảo hành 12 tháng", Description = "1 đổi 1 trong 30 ngày", Price = 30000, UnitType = "đ" },
            }
        },

        // Bắt Đầu Làm Việc
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-1),
            Status = "Bắt Đầu Làm Việc",
            Address = "99 Nguyễn Thị Minh Khai, Q.1, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "HouseCare",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Vệ sinh sofa vải 3 chỗ",
            ServicePrice = 350000, ServiceUnitType = "đ",
            ServiceImages = "/images/sofa.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Khử khuẩn Nano Bạc", Description = "An toàn cho da", Price = 40000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Khử mùi Enzyme",     Description = "Loại bỏ mùi hôi", Price = 30000, UnitType = "đ" },
            }
        },

        // Hoàn thành
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-5),
            Status = "Hoàn thành",
            Address = "789 Trần Hưng Đạo, Q.3, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "Intimate Ziaja Store",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Dung Dịch Vệ Sinh Intimate ZIAJA 200ml",
            ServicePrice = 209000, ServiceUnitType = "đ",
            ServiceImages = "/images/sample1.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Gói quà", Description = "Túi quà + thiệp", Price = 12000, UnitType = "đ" },
            }
        },

        // Đã hủy
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-3),
            Status = "Đã hủy",
            Address = "22 Ung Văn Khiêm, Bình Thạnh, TP.HCM",
            ProviderId = Guid.NewGuid(),
            ProviderName = "CleanUp",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Vệ sinh nhà theo giờ (2h)",
            ServicePrice = 240000, ServiceUnitType = "đ",
            ServiceImages = "/images/clean.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Thêm 30 phút", Description = "Gia hạn thời gian", Price = 60000, UnitType = "đ" },
            }
        },

        // Báo Cáo/Hoàn tiền
        new()
        {
            BookingId = Guid.NewGuid(),
            BookingTime = DateTime.Now.AddDays(-4),
            Status = "Báo Cáo/Hoàn tiền",
            Address = "01 Võ Văn Ngân, TP.Thủ Đức",
            ProviderId = Guid.NewGuid(),
            ProviderName = "TechCare",
            ServiceId = Guid.NewGuid(),
            ServiceTitle = "Sửa chữa – Vệ sinh laptop cơ bản",
            ServicePrice = 300000, ServiceUnitType = "đ",
            ServiceImages = "/images/laptop.png",
            Options = new List<OptionDTO>
            {
                new() { OptionId = Guid.NewGuid(), OptionName = "Thay keo tản nhiệt", Description = "Keo cao cấp", Price = 80000, UnitType = "đ" },
                new() { OptionId = Guid.NewGuid(), OptionName = "Vệ sinh quạt",        Description = "Tháo vệ sinh kỹ", Price = 30000, UnitType = "đ" },
            }
        },
    };

            var vm = new ListHistoryBookingServiceDTOs { Items = items };
            ViewBag.StatusTabs = statuses;
            return View(vm); // View: Areas/Customer/Views/BookingService/ListHistoryBooking.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(CancelBookingRequestDTO model)
        {
            if (!ModelState.IsValid ||
                model.BookingId == Guid.Empty ||
                string.IsNullOrWhiteSpace(model.Reason) ||
                string.IsNullOrWhiteSpace(model.BankName) ||
                string.IsNullOrWhiteSpace(model.AccountHolderName) ||
                string.IsNullOrWhiteSpace(model.BankAccountNumber))
            {
                TempData["ToastError"] = "Vui lòng nhập đầy đủ thông tin hủy đơn.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                // TODO: Gọi API backend để hủy đơn + gửi thông tin hoàn tiền:
                // await _bookingService.CancelAsync(jwtToken, model);

                // Demo: giả lập thành công
                await Task.CompletedTask;

                TempData["ToastSuccess"] = "Hủy đơn thành công. Yêu cầu hoàn tiền sẽ được xử lý trong thời gian sớm nhất.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Bạn cần đăng nhập lại: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Không thể hủy đơn: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
        }

        // GET: /Customer/BookingService/ReportService/{bookingId}
        [HttpGet]
        public IActionResult ReportService(Guid bookingId)
        {
            // TODO: lấy thông tin thực từ DB/API theo bookingId
            // Dữ liệu mock để hiển thị
            var vm = new ComplaintDTO
            {
                BookingId = bookingId,
                ServiceTitle = "Dầu Tắm Oliv 3X Dưỡng Ẩm 650ml",
                ProviderName = "Oliv Official",
                Price = 108800,
                OriginalPrice = 197500,
                ServiceImage = "/images/sample1.png"
            };
            return View(vm); // View: Areas/Customer/Views/BookingService/ReportService.cshtml
        }

        // POST: /Customer/BookingService/SubmitReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(ComplaintDTO model)
        {
            if (model.BookingId == Guid.Empty ||
                string.IsNullOrWhiteSpace(model.ComplaintType) ||
                string.IsNullOrWhiteSpace(model.Description))
            {
                TempData["ToastError"] = "Vui lòng chọn lý do và nhập mô tả.";
                return RedirectToAction(nameof(ReportService), new { bookingId = model.BookingId });
            }

            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                // TODO: Gọi API backend lưu khiếu nại (Complaint)
                // await _complaintService.CreateAsync(jwtToken, model);

                await Task.CompletedTask; // demo

                TempData["ToastSuccess"] = "Gửi báo cáo thành công. Hệ thống sẽ xử lý trong thời gian sớm nhất.";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ToastError"] = $"Bạn cần đăng nhập lại: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Không thể gửi báo cáo: {ex.Message}";
                return RedirectToAction(nameof(ListHistoryBooking));
            }
        }

        // BookingServiceController

        // Có thể đặt ở đầu class (field static) để tái sử dụng
        private static readonly Dictionary<Guid, TermOfServiceDto> _demoTos = new()
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new TermOfServiceDto
            {
                ProviderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ProviderName = "Intimate Ziaja Store",
                Url = "https://example.com/ziaja-terms",
                Description = @"
            <ul>
                <li>Đổi trả trong 7 ngày đối với hàng chưa mở niêm phong.</li>
                <li>Sản phẩm mỹ phẩm tuân thủ quy định của Bộ Y Tế; bảo hành theo chính sách hãng.</li>
                <li>Giao nhanh nội thành TP.HCM 2–4 giờ (trong giờ làm việc).</li>
                <li>Vui lòng xem đầy đủ chính sách và ngoại lệ tại liên kết bên dưới.</li>
            </ul>",
                CreatedAt = DateTime.UtcNow
            },
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new TermOfServiceDto
            {
                ProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProviderName = "(GIFT) Quà Tặng Ziaja",
                Url = "https://example.com/gift-terms",
                Description = @"
            <ul>
                <li>Quà tặng không áp dụng bảo hành; chỉ đổi trong 3 ngày nếu lỗi sản xuất.</li>
                <li>Không hỗ trợ đổi vì lý do thẩm mỹ/chủ quan sau khi đã sử dụng.</li>
                <li>Voucher tặng kèm có thời hạn theo ghi chú trên voucher, không quy đổi tiền mặt.</li>
                <li>Chi tiết điều kiện sử dụng vui lòng xem tại liên kết bên dưới.</li>
            </ul>",
                CreatedAt = DateTime.UtcNow
            }
        };

        [HttpGet]
        public async Task<IActionResult> GetTermsByProvider(Guid providerId)
        {
            try
            {
                var jwtToken = HttpContext.Session.GetString("JWToken");

                // TODO: gọi service thật nếu có
                // var tos = await _providerService.GetTermsAsync(jwtToken, providerId);

                // DEMO: lấy từ dictionary theo providerId
                if (!_demoTos.TryGetValue(providerId, out var tos))
                {
                    tos = new TermOfServiceDto
                    {
                        ProviderId = providerId,
                        ProviderName = "Nhà cung cấp khác",
                        Url = "https://example.com/terms",
                        Description = @"<p>Chính sách chung: đổi trả trong 7 ngày với điều kiện còn nguyên vẹn hóa đơn & phụ kiện. Vui lòng xem chi tiết tại liên kết.</p>",
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Trả HTML thẳng để hiển thị trong modal (Description có thể chứa HTML)
                var html = $@"
            <div>
                <div style=""font-weight:600;margin-bottom:6px"">
                    {System.Net.WebUtility.HtmlEncode(tos.ProviderName)}
                </div>
                <div>{tos.Description}</div>
                <div style=""margin-top:8px"">
                    <a href=""{tos.Url}"" target=""_blank"" rel=""noopener"">Xem đầy đủ điều khoản</a>
                </div>
            </div>";

                return Content(html, "text/html; charset=utf-8");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải điều khoản", error = ex.Message });
            }
        }


        //// GET: /Customer/BookingService/HistoryBookingDetail/{id}
        //[HttpGet]
        //public IActionResult HistoryBookingDetail(Guid id)
        //{
        //    // Lấy dữ liệu mẫu có sẵn
        //    var vm = VHS_frontend.Areas.Customer.Models.BookingServiceDTOs.HistoryBookingDetailDTOs.Sample();

        //    // Gán lại BookingId theo id được click (cho “MÃ ĐƠN HÀNG”/route nhất quán)
        //    vm.BookingId = id;

        //    return View("HistoryBookingDetail", vm);
        //}

        // GET: /Customer/BookingService/HistoryBookingDetail/{id}?status=...
        [HttpGet]
        public IActionResult HistoryBookingDetail(Guid id, string? status)
        {
            // Tạo VM mẫu theo tab/status được click
            var vm = VHS_frontend.Areas.Customer.Models.BookingServiceDTOs.HistoryBookingDetailDTOs
                        .CreateByStatus(status ?? "Tất cả", id);

            return View("HistoryBookingDetail", vm);
        }

        public IActionResult CanceledDetail(Guid id)
        {
            // Lấy đơn đã hủy (demo dùng sample)
            var vm = HistoryBookingDetailDTOs.Sample_Canceled();
            vm.BookingId = id;

            // Thông tin hủy/hoàn tiền đã gửi lúc CancelBooking (demo)
            ViewBag.Cancel = new CancelBookingRequestDTO
            {
                BookingId = id,
                Reason = "Tôi muốn cập nhật địa chỉ/sđt nhận hàng",
                BankName = "Vietcombank",
                AccountHolderName = "NGUYEN VAN A",
                BankAccountNumber = "0123456789"
            };

            return View(vm); // Views/BookingService/CanceledDetail.cshtml
        }


        // Xem chi báo cáo hoàn tiền
        public IActionResult ReportDetail(Guid bookingId)
        {
            // TODO: Lấy ComplaintDTO thực tế từ DB
            var vm = new ComplaintDTO
            {
                BookingId = bookingId,
                ComplaintType = "Hàng/ dịch vụ không như mô tả",
                Description = "Ghế vệ sinh xong vẫn còn vết bẩn nhẹ ở tay vịn.",
                ServiceTitle = "Vệ sinh sofa vải 3 chỗ",
                ProviderName = "HouseCare",
                ServiceImage = "/images/sofa.png",
                OriginalPrice = 390000,
                Price = 350000
            };
            return View(vm);
        }
    }
    }

