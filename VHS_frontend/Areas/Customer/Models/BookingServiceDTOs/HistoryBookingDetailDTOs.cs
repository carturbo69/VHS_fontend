namespace VHS_frontend.Areas.Customer.Models.BookingServiceDTOs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // PART 1/2: Khai báo DTO + Sample (Hoàn thành)
    public partial class HistoryBookingDetailDTOs
    {
        // ===== Booking / Order =====
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;       // Mã đơn
        public string Status { get; set; } = "Chờ xác nhận";          // Chờ xác nhận | Xác nhận | Bắt Đầu Làm Việc | Hoàn thành | Đã hủy | Báo Cáo/Hoàn tiền
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Người nhận & địa chỉ
        public string RecipientFullName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;       // Chuỗi địa chỉ hiển thị
        public string? ShippingTrackingCode { get; set; }             // Mã vận đơn / mã nội bộ

        // Nhà cung cấp / nhân sự thực hiện
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public Guid? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StaffImage { get; set; } // Staff.FaceImage

        // ===== Service (sản phẩm/dịch vụ) =====
        public ServiceInBookingDTO Service { get; set; } = new();
        public List<OptionDTO> Options { get; set; } = new();

        // ===== Giá tiền =====
        public decimal ShippingFee { get; set; }
        public decimal VoucherDiscount { get; set; }
        public string PaymentMethod { get; set; } = "Thanh toán khi nhận hàng"; // hiển thị
        public decimal Subtotal => Service.LineTotal;
        public decimal Total => Subtotal + ShippingFee - VoucherDiscount;

        public decimal PaidAmount { get; set; } // số tiền đã thanh toán (nếu có)

        // ===== Tiến trình / Tracking =====
        public List<TrackingEventDTO> Timeline { get; set; } = new();

        // ===== Factory: dữ liệu mẫu (trạng thái Hoàn thành) =====
        public static HistoryBookingDetailDTOs Sample()
        {
            var bookingId = Guid.NewGuid();
            var providerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var service = new ServiceInBookingDTO
            {
                ServiceId = Guid.NewGuid(),
                Title = "Combo 2 mặt nạ nghệ Hưng Yên Cocoon 30ml",
                Image = "/images/sample1.png",
                UnitPrice = 179000,
                Quantity = 1,
                UnitType = "đ"
            };

            var opts = new List<OptionDTO>
            {
                new()
                {
                    OptionId = Guid.NewGuid(),
                    OptionName = "Giao hỏa tốc",
                    Description = "Giao trong ngày nội thành",
                    Price = 30000, UnitType = "đ"
                },
                new()
                {
                    OptionId = Guid.NewGuid(),
                    OptionName = "Gói quà",
                    Description = "Túi quà + thiệp",
                    Price = 12000, UnitType = "đ"
                }
            };
            service.Options = opts;
            service.IncludeOptionPriceToLineTotal = false; // giữ nguyên giống ảnh

            var created = new DateTime(DateTime.Now.Year, 9, 8, 21, 27, 0);
            var startWork = new DateTime(DateTime.Now.Year, 9, 9, 7, 1, 0);
            var delivering = new DateTime(DateTime.Now.Year, 9, 10, 8, 4, 0);
            var delivered = new DateTime(DateTime.Now.Year, 9, 10, 11, 35, 0);
            var finished = new DateTime(DateTime.Now.Year, 10, 13, 23, 59, 0);

            var vm = new HistoryBookingDetailDTOs
            {
                BookingId = bookingId,
                BookingCode = "2509081627M4VX",
                Status = "Hoàn thành",
                CreatedAt = created,
                CompletedAt = finished,

                RecipientFullName = "Tố Tố",
                RecipientPhone = "(+84) 365 039 433",
                AddressLine = "118/22 Hẻm 107 Đường 3/2, Phường Hưng Lợi, Quận Ninh Kiều, Cần Thơ",

                ShippingTrackingCode = "SPXVN052392835709",
                ProviderId = providerId,
                ProviderName = "Cocoon Vietnam Chính Hãng",
                StaffId = Guid.NewGuid(),
                StaffName = "Trần Thị Support",
                StaffImage = "/images/staff/tran-thi-support.jpg",

                Service = service,
                Options = opts,

                ShippingFee = 28700,
                VoucherDiscount = 35800,
                PaymentMethod = "Thanh toán khi nhận hàng",
                PaidAmount = 50000m,

                Timeline = new List<TrackingEventDTO>
                {
                    new()
                    {
                        Time = created, Code = "CREATED",
                        Title = "Đơn Hàng Đã Đặt",
                        Description = "Đã đặt lúc 21:27 08-09",
                        Proofs = new List<MediaProofDTO>()
                    },
                    new()
                    {
                        Time = created.AddMinutes(5), Code = "CONFIRMED",
                        Title = "Đã Xác Nhận Thông Tin Thanh Toán",
                        Description = "Shop xác nhận đơn lúc 21:32 08-09",
                    },
                    new()
                    {
                        Time = startWork, Code = "START_WORK",
                        Title = "Bắt Đầu Làm Việc",
                        Description = "Nhân viên bắt đầu xử lý/đóng gói.",
                        Proofs = new List<MediaProofDTO>
                        {
                            new() { MediaType="image", Url="/images/proofs/start-1.jpg", Caption="Bàn giao nhiệm vụ" },
                            new() { MediaType="image", Url="/images/proofs/start-2.jpg", Caption="Đóng gói" }
                        }
                    },
                    new()
                    {
                        Time = finished, Code = "COMPLETED",
                        Title = "Đơn Hàng Đã Hoàn Thành",
                        Description = "Khách đã xác nhận hoàn thành.",
                        Proofs = new List<MediaProofDTO>
                        {
                            new() { MediaType="image", Url="/images/proofs/done-1.jpg", Caption="Ảnh hoàn tất" },
                            new() { MediaType="image", Url="/images/proofs/done-2.jpg", Caption="Biên bản nghiệm thu" }
                        }
                    }
                }
            };

            return vm;
        }
    }

    // ====== Sub DTOs ======
    public class ServiceInBookingDTO
    {
        public Guid ServiceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = "/images/placeholder.png";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public string UnitType { get; set; } = "đ";
        public List<OptionDTO> Options { get; set; } = new();
        public bool IncludeOptionPriceToLineTotal { get; set; } = false;

        public decimal OptionsTotal => Options?.Sum(o => o.Price) ?? 0m;
        public decimal LineTotal => (UnitPrice * Quantity) + (IncludeOptionPriceToLineTotal ? OptionsTotal : 0m);
    }

    public class TrackingEventDTO
    {
        public DateTime Time { get; set; }
        public string Code { get; set; } = string.Empty;     // CREATED / CONFIRMED / START_WORK / DELIVERED / COMPLETED ...
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<MediaProofDTO> Proofs { get; set; } = new();
    }

    public class MediaProofDTO
    {
        public string MediaType { get; set; } = "image";     // image | video
        public string Url { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }

    // PART 2/2: Các sample theo từng trạng thái + helper chọn theo status
    public partial class HistoryBookingDetailDTOs
    {
        public static HistoryBookingDetailDTOs Sample_WaitingConfirm()
        {
            var now = DateTime.Now;
            var created = now.AddHours(-6);

            var service = new ServiceInBookingDTO
            {
                ServiceId = Guid.NewGuid(),
                Title = "Dung Dịch Vệ Sinh Intimate With Lactic Acid ZIAJA 200ml",
                Image = "/images/sample1.png",
                UnitPrice = 209000,
                Quantity = 1,
                UnitType = "đ",
                Options = new List<OptionDTO>
                {
                    new() { OptionId = Guid.NewGuid(), OptionName = "Gói đóng gói an toàn", Description = "Bọc chống sốc + thùng carton", Price = 10000, UnitType = "đ" },
                    new() { OptionId = Guid.NewGuid(), OptionName = "Giao nhanh 2H",       Description = "Ưu tiên điều phối nhanh",     Price = 15000, UnitType = "đ" },
                },
                IncludeOptionPriceToLineTotal = false
            };

            return new HistoryBookingDetailDTOs
            {
                BookingId = Guid.NewGuid(),
                BookingCode = "WCONF" + now.ToString("HHmmss"),
                Status = "Chờ xác nhận",
                CreatedAt = created,

                RecipientFullName = "Nguyễn A",
                RecipientPhone = "0900 000 001",
                AddressLine = "12 Lý Thái Tổ, Q.10, TP.HCM",

                ProviderId = Guid.NewGuid(),
                ProviderName = "Intimate Ziaja Store",

                Service = service,
                Options = service.Options,

                ShippingFee = 15000,
                VoucherDiscount = 0,
                PaymentMethod = "Thanh toán khi nhận hàng",
                PaidAmount = 0,

                Timeline = new List<TrackingEventDTO>
                {
                    new()
                    {
                        Time = created, Code = "CREATED",
                        Title = "Đơn Hàng Đã Đặt",
                        Description = $"Đã đặt lúc {created:HH:mm dd-MM}"
                    },
                    new()
                    {
                        Time = created.AddMinutes(2), Code = "PENDING_CONFIRM",
                        Title = "Chờ xác nhận",
                        Description = "Shop đang kiểm tra tồn kho và thông tin đơn."
                    }
                }
            };
        }

        public static HistoryBookingDetailDTOs Sample_Confirmed()
        {
            var now = DateTime.Now;
            var created = now.AddHours(-10);
            var confirmed = created.AddMinutes(8);

            var service = new ServiceInBookingDTO
            {
                ServiceId = Guid.NewGuid(),
                Title = "Vệ sinh máy lạnh treo tường",
                Image = "/images/ac.png",
                UnitPrice = 150000,
                Quantity = 1,
                UnitType = "đ",
                Options = new List<OptionDTO>
                {
                    new() { OptionId = Guid.NewGuid(), OptionName = "Vệ sinh dàn nóng", Description = "Bổ sung dàn nóng", Price = 50000, UnitType = "đ" },
                    new() { OptionId = Guid.NewGuid(), OptionName = "Bảo hành 7 ngày",  Description = "Quay lại xử lý",  Price = 20000, UnitType = "đ" },
                },
                IncludeOptionPriceToLineTotal = false
            };

            return new HistoryBookingDetailDTOs
            {
                BookingId = Guid.NewGuid(),
                BookingCode = "CONF" + now.ToString("HHmmss"),
                Status = "Xác Nhận",
                CreatedAt = created,

                RecipientFullName = "Lê B",
                RecipientPhone = "0900 000 002",
                AddressLine = "23 Hoàng Diệu, Q.4, TP.HCM",

                ProviderId = Guid.NewGuid(),
                ProviderName = "Green House",

                Service = service,
                Options = service.Options,

                ShippingFee = 20000,
                VoucherDiscount = 0,
                PaymentMethod = "Thanh toán khi hoàn thành",
                PaidAmount = 0,

                Timeline = new List<TrackingEventDTO>
                {
                    new() { Time = created,   Code = "CREATED",   Title = "Đơn Hàng Đã Đặt", Description = $"Đã đặt lúc {created:HH:mm dd-MM}" },
                    new() { Time = confirmed, Code = "CONFIRMED", Title = "Đã Xác Nhận",     Description = "Shop xác nhận đơn và dự kiến điều phối kỹ thuật." }
                }
            };
        }

        public static HistoryBookingDetailDTOs Sample_InProgress()
        {
            var now = DateTime.Now;
            var created = now.AddDays(-1).AddHours(-2);
            var confirmed = created.AddMinutes(15);
            var startWork = confirmed.AddHours(9);

            var service = new ServiceInBookingDTO
            {
                ServiceId = Guid.NewGuid(),
                Title = "Vệ sinh sofa vải 3 chỗ",
                Image = "/images/sofa.png",
                UnitPrice = 350000,
                Quantity = 1,
                UnitType = "đ",
                Options = new List<OptionDTO>
                {
                    new() { OptionId = Guid.NewGuid(), OptionName = "Khử khuẩn Nano Bạc", Description = "An toàn cho da", Price = 40000, UnitType = "đ" },
                    new() { OptionId = Guid.NewGuid(), OptionName = "Khử mùi Enzyme",     Description = "Loại bỏ mùi hôi", Price = 30000, UnitType = "đ" },
                },
                IncludeOptionPriceToLineTotal = false
            };

            return new HistoryBookingDetailDTOs
            {
                BookingId = Guid.NewGuid(),
                BookingCode = "WORK" + now.ToString("HHmmss"),
                Status = "Bắt Đầu Làm Việc",
                CreatedAt = created,

                RecipientFullName = "Trần C",
                RecipientPhone = "0900 000 003",
                AddressLine = "99 Nguyễn Thị Minh Khai, Q.1, TP.HCM",

                ProviderId = Guid.NewGuid(),
                ProviderName = "HouseCare",
                StaffId = Guid.NewGuid(),
                StaffName = "Trần Thị Support",
                StaffImage = "/images/staff/tran-thi-support.jpg",

                Service = service,
                Options = service.Options,

                ShippingFee = 0,
                VoucherDiscount = 0,
                PaymentMethod = "Tiền mặt",
                PaidAmount = 0,

                Timeline = new List<TrackingEventDTO>
                {
                    new() { Time = created,   Code = "CREATED",   Title = "Đơn Hàng Đã Đặt",   Description = $"Đã đặt lúc {created:HH:mm dd-MM}" },
                    new() { Time = confirmed, Code = "CONFIRMED", Title = "Đã Xác Nhận",       Description = "Đã phân công nhân sự." },
                    new()
                    {
                        Time = startWork, Code = "START_WORK", Title = "Bắt Đầu Làm Việc",
                        Description = "Nhân viên đến địa điểm và bắt đầu xử lý.",
                        Proofs = new List<MediaProofDTO>
                        {
                            new() { MediaType = "image", Url="/images/proofs/start-1.jpg", Caption="Check-in" },
                            new() { MediaType = "image", Url="/images/proofs/start-2.jpg", Caption="Chuẩn bị dụng cụ" }
                        }
                    }
                }
            };
        }

        public static HistoryBookingDetailDTOs Sample_Canceled()
        {
            var now = DateTime.Now;
            var created = now.AddDays(-3).AddHours(-4);
            var canceled = created.AddHours(2);

            var service = new ServiceInBookingDTO
            {
                ServiceId = Guid.NewGuid(),
                Title = "Vệ sinh nhà theo giờ (2h)",
                Image = "/images/clean.png",
                UnitPrice = 240000,
                Quantity = 1,
                UnitType = "đ",
                Options = new List<OptionDTO>
                {
                    new() { OptionId = Guid.NewGuid(), OptionName = "Thêm 30 phút", Description = "Gia hạn thời gian", Price = 60000, UnitType = "đ" },
                },
                IncludeOptionPriceToLineTotal = false
            };

            return new HistoryBookingDetailDTOs
            {
                BookingId = Guid.NewGuid(),
                BookingCode = "CANCEL" + now.ToString("HHmmss"),
                Status = "Đã hủy",
                CreatedAt = created,
                CompletedAt = canceled,

                RecipientFullName = "Phạm D",
                RecipientPhone = "0900 000 004",
                AddressLine = "22 Ung Văn Khiêm, Bình Thạnh, TP.HCM",

                ProviderId = Guid.NewGuid(),
                ProviderName = "CleanUp",

                Service = service,
                Options = service.Options,

                ShippingFee = 0,
                VoucherDiscount = 0,
                PaymentMethod = "Chuyển khoản",
                PaidAmount = 0,

                Timeline = new List<TrackingEventDTO>
                {
                    new() { Time = created,  Code = "CREATED",  Title = "Đơn Hàng Đã Đặt", Description = $"Đã đặt lúc {created:HH:mm dd-MM}" },
                    new() { Time = canceled, Code = "CANCELED", Title = "Đơn Hàng Đã Hủy", Description = "Khách yêu cầu hủy trước khi thực hiện (lý do: thay đổi lịch)." }
                }
            };
        }

        public static HistoryBookingDetailDTOs Sample_ReportRefund()
        {
            var now = DateTime.Now;
            var created = now.AddDays(-4).AddHours(-3);
            var delivered = created.AddDays(1).AddHours(2);
            var complaint = delivered.AddHours(5);

            var service = new ServiceInBookingDTO
            {
                ServiceId = Guid.NewGuid(),
                Title = "Sửa chữa – Vệ sinh laptop cơ bản",
                Image = "/images/laptop.png",
                UnitPrice = 300000,
                Quantity = 1,
                UnitType = "đ",
                Options = new List<OptionDTO>
                {
                    new() { OptionId = Guid.NewGuid(), OptionName = "Thay keo tản nhiệt", Description = "Keo cao cấp", Price = 80000, UnitType = "đ" },
                    new() { OptionId = Guid.NewGuid(), OptionName = "Vệ sinh quạt",        Description = "Tháo vệ sinh kỹ", Price = 30000, UnitType = "đ" },
                },
                IncludeOptionPriceToLineTotal = false
            };

            return new HistoryBookingDetailDTOs
            {
                BookingId = Guid.NewGuid(),
                BookingCode = "RPT" + now.ToString("HHmmss"),
                Status = "Báo Cáo/Hoàn tiền",
                CreatedAt = created,

                RecipientFullName = "Đỗ E",
                RecipientPhone = "0900 000 005",
                AddressLine = "01 Võ Văn Ngân, TP.Thủ Đức",

                ProviderId = Guid.NewGuid(),
                ProviderName = "TechCare",

                Service = service,
                Options = service.Options,

                ShippingFee = 0,
                VoucherDiscount = 0,
                PaymentMethod = "Chuyển khoản",
                PaidAmount = 0,

                ShippingTrackingCode = "GHN-123456789",

                Timeline = new List<TrackingEventDTO>
                {
                    new() { Time = created,                   Code = "CREATED",        Title = "Đơn Hàng Đã Đặt",      Description = $"Đã đặt lúc {created:HH:mm dd-MM}" },
                    new() { Time = delivered,                 Code = "DELIVERED",      Title = "Đã giao",              Description = "Dịch vụ/thiết bị đã được bàn giao." },
                    new() { Time = complaint,                 Code = "COMPLAINT",      Title = "Mở báo cáo/hoàn tiền", Description = "Khách mở báo cáo do chưa đạt kỳ vọng." },
                    new() { Time = complaint.AddMinutes(10),  Code = "REFUND_PENDING", Title = "Đang xử lý hoàn tiền", Description = "Hệ thống ghi nhận yêu cầu hoàn tiền." }
                }
            };
        }

        /// <summary>
        /// Helper: tạo sample theo Status tab (đặt BookingId theo tham số để route/link nhất quán).
        /// </summary>
        public static HistoryBookingDetailDTOs CreateByStatus(string? status, Guid? bookingIdOverride = null)
        {
            var s = (status ?? "").Trim();

            // Chuẩn hoá: bỏ dấu + lowercase
            string Normalize(string input)
            {
                var stFormD = input.Normalize(System.Text.NormalizationForm.FormD);
                var sb = new System.Text.StringBuilder();
                foreach (var ch in stFormD)
                {
                    var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                        sb.Append(ch);
                }
                return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
            }

            var key = Normalize(s);

            HistoryBookingDetailDTOs vm = key switch
            {
                "cho xac nhan" => Sample_WaitingConfirm(),
                "xac nhan" => Sample_Confirmed(),
                "bat dau lam viec" => Sample_InProgress(),
                "hoan thanh" => Sample(),
                "da huy" => Sample_Canceled(),
                "bao cao/hoan tien" => Sample_ReportRefund(),
                _ => Sample()
            };

            if (bookingIdOverride is { } bid && bid != Guid.Empty)
                vm.BookingId = bid;

            return vm;
        }

    }
}
