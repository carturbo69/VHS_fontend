using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Feedback;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminFeedbackController : Controller
    {
        private readonly AdminFeedbackService _feedbackService;

        public AdminFeedbackController(AdminFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        public async Task<IActionResult> Index()
        {
            var accountId = HttpContext.Session.GetString("AccountID");
            var role = HttpContext.Session.GetString("Role");

            // Kiểm tra Session
            if (string.IsNullOrEmpty(accountId) ||
                !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            
            // Set authentication token
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _feedbackService.SetBearerToken(token);
            
            try
            {
                // Dữ liệu feedback ảo để test
                var feedbacks = GetMockFeedbacks();
                return View(feedbacks);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải danh sách feedback: " + ex.Message;
                return View(new List<FeedbackDTO>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(token))
                    _feedbackService.SetBearerToken(token);

                var success = await _feedbackService.DeleteAsync(id);
                if (success)
                {
                    TempData["Success"] = "Xóa feedback thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa feedback!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa feedback: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Hide(Guid id)
        {
            try
            {
                var token = HttpContext.Session.GetString("JWToken");
                if (!string.IsNullOrWhiteSpace(token))
                    _feedbackService.SetBearerToken(token);

                var success = await _feedbackService.HideAsync(id);
                if (success)
                {
                    TempData["Success"] = "Ẩn feedback thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể ẩn feedback!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi ẩn feedback: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private List<FeedbackDTO> GetMockFeedbacks()
        {
            return new List<FeedbackDTO>
            {
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Nguyễn Văn An",
                    CustomerEmail = "an.nguyen@gmail.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Vệ sinh nhà cửa",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Công ty Vệ sinh ABC",
                    Rating = 5,
                    Comment = "Dịch vụ rất tốt, nhân viên chuyên nghiệp và thân thiện. Nhà cửa sạch sẽ như mới. Sẽ tiếp tục sử dụng dịch vụ này!",
                    Images = new List<string> 
                    { 
                        "https://images.unsplash.com/photo-1581578731548-c6a0c3f2f6d6?w=400&h=300&fit=crop",
                        "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=300&fit=crop"
                    },
                    CreatedAt = DateTime.Now.AddDays(-2),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Trần Thị Bình",
                    CustomerEmail = "binh.tran@outlook.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Sửa chữa điện",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Thợ điện Minh",
                    Rating = 4,
                    Comment = "Thợ điện rất giỏi, sửa nhanh và giá cả hợp lý. Tuy nhiên hơi muộn một chút so với giờ hẹn.",
                    Images = new List<string> 
                    { 
                        "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?w=400&h=300&fit=crop"
                    },
                    CreatedAt = DateTime.Now.AddDays(-5),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Lê Văn Cường",
                    CustomerEmail = "cuong.le@yahoo.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Làm vườn",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Dịch vụ làm vườn XYZ",
                    Rating = 3,
                    Comment = "Dịch vụ ổn nhưng giá hơi cao. Nhân viên làm việc cẩn thận nhưng hơi chậm.",
                    Images = new List<string> 
                    { 
                        "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=400&h=300&fit=crop",
                        "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?w=400&h=300&fit=crop"
                    },
                    CreatedAt = DateTime.Now.AddDays(-7),
                    IsVisible = false,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Phạm Thị Dung",
                    CustomerEmail = "dung.pham@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Vệ sinh văn phòng",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Công ty Vệ sinh DEF",
                    Rating = 5,
                    Comment = "Tuyệt vời! Văn phòng sạch sẽ, nhân viên chuyên nghiệp. Đúng giờ và giá cả hợp lý. Rất hài lòng!",
                    CreatedAt = DateTime.Now.AddDays(-10),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Hoàng Văn Em",
                    CustomerEmail = "em.hoang@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Sửa chữa ống nước",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Thợ ống nước Gia",
                    Rating = 2,
                    Comment = "Dịch vụ không tốt, thợ đến muộn và làm việc không cẩn thận. Vẫn còn rò rỉ nước sau khi sửa.",
                    CreatedAt = DateTime.Now.AddDays(-12),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Võ Thị Phương",
                    CustomerEmail = "phuong.vo@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Dọn dẹp sau xây dựng",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Công ty Vệ sinh HIJ",
                    Rating = 4,
                    Comment = "Nhân viên làm việc rất cẩn thận và tỉ mỉ. Nhà cửa sạch sẽ hoàn toàn. Chỉ hơi đắt một chút.",
                    CreatedAt = DateTime.Now.AddDays(-15),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Đặng Văn Giang",
                    CustomerEmail = "giang.dang@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Vệ sinh máy lạnh",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Dịch vụ máy lạnh KLM",
                    Rating = 1,
                    Comment = "Dịch vụ rất tệ! Thợ không chuyên nghiệp, làm hỏng máy lạnh và không chịu trách nhiệm. Không bao giờ sử dụng lại!",
                    CreatedAt = DateTime.Now.AddDays(-20),
                    IsVisible = false,
                    IsDeleted = true
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Bùi Thị Hoa",
                    CustomerEmail = "hoa.bui@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Làm sạch thảm",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Dịch vụ thảm NOP",
                    Rating = 5,
                    Comment = "Thảm sạch như mới! Dịch vụ chuyên nghiệp, giá cả hợp lý. Sẽ giới thiệu cho bạn bè.",
                    CreatedAt = DateTime.Now.AddDays(-25),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Ngô Văn Ích",
                    CustomerEmail = "ich.ngo@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Vệ sinh cửa kính",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Cửa kính sạch QRS",
                    Rating = 3,
                    Comment = "Dịch vụ ổn, kính sạch nhưng giá hơi cao so với thị trường. Nhân viên thân thiện.",
                    CreatedAt = DateTime.Now.AddDays(-30),
                    IsVisible = true,
                    IsDeleted = false
                },
                new FeedbackDTO
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Lý Thị Kim",
                    CustomerEmail = "kim.ly@email.com",
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Vệ sinh nhà xưởng",
                    ProviderId = Guid.NewGuid(),
                    ProviderName = "Công ty Vệ sinh TUV",
                    Rating = 4,
                    Comment = "Nhà xưởng sạch sẽ hoàn toàn. Nhân viên làm việc hiệu quả và đúng giờ. Rất hài lòng với dịch vụ.",
                    CreatedAt = DateTime.Now.AddDays(-35),
                    IsVisible = true,
                    IsDeleted = false
                }
            };
        }
    }
}
