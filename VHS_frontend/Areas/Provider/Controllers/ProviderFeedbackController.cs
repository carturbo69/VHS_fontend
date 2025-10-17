using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Feedback;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderFeedbackController : Controller
    {
        public IActionResult Index()
        {
            // Dữ liệu ảo cho feedback nhóm theo dịch vụ
            var model = new ProviderFeedbackViewModel
            {
                ServiceFeedbacks = new List<ServiceFeedbackGroup>
                {
                    new ServiceFeedbackGroup
                    {
                        ServiceId = 1,
                        ServiceName = "Vệ sinh nhà cửa",
                        ServiceIcon = "bi-house-heart",
                        AverageRating = 4.5,
                        TotalFeedbacks = 12,
                        Feedbacks = new List<CustomerFeedback>
                        {
                            new CustomerFeedback
                            {
                                Id = 1,
                                CustomerName = "Nguyễn Văn An",
                                CustomerAvatar = "NV",
                                Rating = 5,
                                Comment = "Dịch vụ rất tốt, nhân viên chuyên nghiệp và tận tâm. Tôi rất hài lòng!",
                                CreatedAt = DateTime.Now.AddDays(-2),
                                IsVerified = true,
                                Images = new List<FeedbackImage>
                                {
                                    new FeedbackImage { Id = 1, Url = "https://images.unsplash.com/photo-1581578731548-c6a0c3f2fcc0?w=400", Alt = "Nhà cửa sạch sẽ", ThumbnailUrl = "https://images.unsplash.com/photo-1581578731548-c6a0c3f2fcc0?w=150" },
                                    new FeedbackImage { Id = 2, Url = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400", Alt = "Phòng khách được dọn dẹp", ThumbnailUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=150" }
                                },
                                Reply = new ProviderReply
                                {
                                    Id = 1,
                                    Content = "Cảm ơn anh đã tin tưởng và đánh giá cao dịch vụ của chúng tôi! Chúng tôi sẽ tiếp tục nỗ lực để mang đến trải nghiệm tốt nhất cho khách hàng.",
                                    CreatedAt = DateTime.Now.AddDays(-1),
                                    ProviderName = "VHS Provider"
                                }
                            },
                            new CustomerFeedback
                            {
                                Id = 2,
                                CustomerName = "Trần Thị Bình",
                                CustomerAvatar = "TB",
                                Rating = 4,
                                Comment = "Nhà cửa sạch sẽ, giá cả hợp lý. Sẽ sử dụng lại dịch vụ.",
                                CreatedAt = DateTime.Now.AddDays(-5),
                                IsVerified = true,
                                Images = new List<FeedbackImage>
                                {
                                    new FeedbackImage { Id = 3, Url = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400", Alt = "Phòng ngủ được dọn dẹp", ThumbnailUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=150" }
                                }
                            },
                            new CustomerFeedback
                            {
                                Id = 3,
                                CustomerName = "Lê Văn Cường",
                                CustomerAvatar = "LC",
                                Rating = 5,
                                Comment = "Excellent service! Highly recommended.",
                                CreatedAt = DateTime.Now.AddDays(-7),
                                IsVerified = false
                            }
                        }
                    },
                    new ServiceFeedbackGroup
                    {
                        ServiceId = 2,
                        ServiceName = "Sửa chữa điện nước",
                        ServiceIcon = "bi-tools",
                        AverageRating = 4.2,
                        TotalFeedbacks = 8,
                        Feedbacks = new List<CustomerFeedback>
                        {
                            new CustomerFeedback
                            {
                                Id = 4,
                                CustomerName = "Phạm Thị Dung",
                                CustomerAvatar = "PD",
                                Rating = 4,
                                Comment = "Thợ sửa chữa nhanh chóng và tận tâm. Giá cả hợp lý.",
                                CreatedAt = DateTime.Now.AddDays(-1),
                                IsVerified = true,
                                Images = new List<FeedbackImage>
                                {
                                    new FeedbackImage { Id = 4, Url = "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=400", Alt = "Công việc sửa chữa điện", ThumbnailUrl = "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=150" },
                                    new FeedbackImage { Id = 5, Url = "https://images.unsplash.com/photo-1581094794329-c8112a89af12?w=400", Alt = "Thợ đang sửa chữa", ThumbnailUrl = "https://images.unsplash.com/photo-1581094794329-c8112a89af12?w=150" },
                                    new FeedbackImage { Id = 6, Url = "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=400", Alt = "Kết quả sau sửa chữa", ThumbnailUrl = "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=150" }
                                },
                                Reply = new ProviderReply
                                {
                                    Id = 2,
                                    Content = "Cảm ơn chị đã đánh giá tích cực! Đội ngũ thợ của chúng tôi luôn cố gắng hoàn thành công việc một cách nhanh chóng và chuyên nghiệp nhất.",
                                    CreatedAt = DateTime.Now.AddHours(-12),
                                    ProviderName = "VHS Provider"
                                }
                            },
                            new CustomerFeedback
                            {
                                Id = 5,
                                CustomerName = "Hoàng Văn Em",
                                CustomerAvatar = "HE",
                                Rating = 5,
                                Comment = "Sửa chữa rất chuyên nghiệp, thời gian nhanh. Cảm ơn!",
                                CreatedAt = DateTime.Now.AddDays(-3),
                                IsVerified = true
                            }
                        }
                    },
                    new ServiceFeedbackGroup
                    {
                        ServiceId = 3,
                        ServiceName = "Dọn dẹp văn phòng",
                        ServiceIcon = "bi-building",
                        AverageRating = 4.7,
                        TotalFeedbacks = 15,
                        Feedbacks = new List<CustomerFeedback>
                        {
                            new CustomerFeedback
                            {
                                Id = 6,
                                CustomerName = "Vũ Thị Phương",
                                CustomerAvatar = "VP",
                                Rating = 5,
                                Comment = "Văn phòng sạch sẽ, nhân viên lịch sự. Dịch vụ tuyệt vời!",
                                CreatedAt = DateTime.Now.AddDays(-4),
                                IsVerified = true,
                                Images = new List<FeedbackImage>
                                {
                                    new FeedbackImage { Id = 7, Url = "https://images.unsplash.com/photo-1497366216548-37526070297c?w=400", Alt = "Văn phòng sau khi dọn dẹp", ThumbnailUrl = "https://images.unsplash.com/photo-1497366216548-37526070297c?w=150" },
                                    new FeedbackImage { Id = 8, Url = "https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=400", Alt = "Không gian làm việc sạch sẽ", ThumbnailUrl = "https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=150" }
                                },
                                Reply = new ProviderReply
                                {
                                    Id = 3,
                                    Content = "Rất vui khi nhận được phản hồi tích cực từ chị! Chúng tôi luôn đặt chất lượng dịch vụ và sự hài lòng của khách hàng lên hàng đầu.",
                                    CreatedAt = DateTime.Now.AddDays(-3),
                                    ProviderName = "VHS Provider"
                                }
                            },
                            new CustomerFeedback
                            {
                                Id = 7,
                                CustomerName = "Đặng Văn Quang",
                                CustomerAvatar = "DQ",
                                Rating = 4,
                                Comment = "Chất lượng dịch vụ tốt, giá cả phải chăng.",
                                CreatedAt = DateTime.Now.AddDays(-6),
                                IsVerified = true
                            },
                            new CustomerFeedback
                            {
                                Id = 8,
                                CustomerName = "Bùi Thị Rượu",
                                CustomerAvatar = "BR",
                                Rating = 5,
                                Comment = "Rất hài lòng với dịch vụ. Sẽ giới thiệu cho bạn bè.",
                                CreatedAt = DateTime.Now.AddDays(-8),
                                IsVerified = false
                            }
                        }
                    },
                    new ServiceFeedbackGroup
                    {
                        ServiceId = 4,
                        ServiceName = "Bảo trì máy lạnh",
                        ServiceIcon = "bi-thermometer-snow",
                        AverageRating = 4.0,
                        TotalFeedbacks = 5,
                        Feedbacks = new List<CustomerFeedback>
                        {
                            new CustomerFeedback
                            {
                                Id = 9,
                                CustomerName = "Ngô Văn Sơn",
                                CustomerAvatar = "NS",
                                Rating = 4,
                                Comment = "Bảo trì tốt, máy lạnh chạy êm hơn sau khi bảo trì.",
                                CreatedAt = DateTime.Now.AddDays(-9),
                                IsVerified = true
                            },
                            new CustomerFeedback
                            {
                                Id = 10,
                                CustomerName = "Dương Thị Tuyết",
                                CustomerAvatar = "DT",
                                Rating = 4,
                                Comment = "Thợ kỹ thuật chuyên nghiệp, giải thích rõ ràng.",
                                CreatedAt = DateTime.Now.AddDays(-11),
                                IsVerified = true
                            }
                        }
                    }
                },
                OverallStats = new FeedbackStats
                {
                    TotalFeedbacks = 40,
                    AverageRating = 4.35,
                    FiveStarCount = 18,
                    FourStarCount = 15,
                    ThreeStarCount = 5,
                    TwoStarCount = 2,
                    OneStarCount = 0
                }
            };

            ViewData["Title"] = "Phản hồi khách hàng";
            return View(model);
        }
    }
}
