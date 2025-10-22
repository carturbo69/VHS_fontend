using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using VHS_frontend.Areas.Customer.Models.ReviewDTOs;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ReviewCustomerController : Controller
    {
        // Dữ liệu demo
        private static readonly List<ReviewListItemDto> _reviews = new()
        {
            new ReviewListItemDto
            {
                ReviewId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FullName = "hoaianh2003",
                ServiceThumbnailUrl = "/images/sample1.png",
                Rating = 5,
                Comment = "mùi hương: ok",
                Reply = "Shop xin cảm ơn đánh giá của bạn...",
                ServiceTitle = "Dung Dịch Vệ Sinh Intimate...",
                UserAvatarUrl = "/images/sample1.png",
                CreatedAt = DateTime.Now.AddDays(-3),
                LikeCount = 0,
                ReviewImageUrls = new List<string>
                {
                    "/images/reviews/r1-1.jpg",
                    "/images/reviews/r1-2.jpg",
                    "/images/reviews/r1-3.jpg",
                },
            },
            new ReviewListItemDto
            {
                ReviewId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FullName = "hoaianh2003",
                ServiceThumbnailUrl = "/images/sample1.png",
                Rating = 4,
                Comment = "Đóng gói chắc chắn, giao nhanh.",
                Reply = null,
                ServiceTitle = "Dịch vụ vệ sinh máy lạnh tận nơi",
                UserAvatarUrl = "/images/sample1.png",
                CreatedAt = DateTime.Now.AddMonths(-1),
                LikeCount = 2,
                ReviewImageUrls = new List<string> { "/images/reviews/r3-1.jpg" },
            }
        };

        // GET: /Customer/ReviewCustomer
        public IActionResult Index()
        {
            return View(_reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EditReviewDto dto)
        {
            // Nếu review đã có reply => không cho vào flow Edit nữa
            var reviewForCheck = _reviews.FirstOrDefault(r => r.ReviewId == dto.ReviewId);
            if (reviewForCheck == null)
            {
                TempData["Toast"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }
            if (!string.IsNullOrWhiteSpace(reviewForCheck.Reply))
            {
                TempData["Toast"] = "Đánh giá đã có phản hồi, không thể sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu model invalid => trả về View + auto open như cũ
            if (!ModelState.IsValid)
            {
                ViewBag.OpenModalId = dto.ReviewId;
                ViewBag.OpenModalDto = new
                {
                    ReviewId = dto.ReviewId,
                    Rating = dto.Rating,
                    Comment = dto.Comment ?? string.Empty,
                    RemoveImages = dto.RemoveImages ?? new List<string>()
                };
                return View("Index", _reviews);
            }

            // Từ đây trở đi chắc chắn review chưa có Reply
            var review = reviewForCheck;

            review.Rating = Math.Clamp(dto.Rating, 1, 5);
            review.Comment = dto.Comment ?? string.Empty;

            if (dto.RemoveImages?.Any() == true)
            {
                review.ReviewImageUrls = review.ReviewImageUrls
                    .Where(u => !dto.RemoveImages.Contains(u, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            if (dto.NewImages != null && dto.NewImages.Count > 0)
            {
                foreach (var file in dto.NewImages)
                {
                    if (file?.Length > 0)
                    {
                        var fakeUrl = "/uploads/" + file.FileName;
                        review.ReviewImageUrls.Add(fakeUrl);
                    }
                }
            }

            TempData["Toast"] = "Cập nhật đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var review = _reviews.FirstOrDefault(r => r.ReviewId == id);
            if (review == null)
            {
                TempData["Toast"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(review.Reply))
            {
                TempData["Toast"] = "Đánh giá đã có phản hồi, không thể xoá.";
                return RedirectToAction(nameof(Index));
            }

            _reviews.Remove(review);
            TempData["Toast"] = "Đã xóa đánh giá.";
            return RedirectToAction(nameof(Index));
        }

    }
}