using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using VHS_frontend.Areas.Customer.Models.ReviewDTOs;
using VHS_frontend.Services.Customer;

namespace VHS_frontend.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ReviewCustomerController : Controller
    {

        private readonly ReviewServiceCustomer _reviewServiceCustomer;

        public ReviewCustomerController(ReviewServiceCustomer reviewServiceCustomer)
        {
            _reviewServiceCustomer = reviewServiceCustomer;
        }


        // Lấy AccountId từ Claims hoặc Session (bạn đã có sẵn hàm này)
        private Guid GetAccountId()
        {
            var idStr = User.FindFirstValue("AccountID") ?? HttpContext.Session.GetString("AccountID");
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }

        // Lấy JWT để gọi API (tuỳ app bạn lưu ở đâu)
        private string? GetJwtToken()
        {
            // Thử cookie trước
            var token = Request.Cookies["JWToken"];
            if (!string.IsNullOrWhiteSpace(token)) return token;

            // Rồi tới session
            token = HttpContext.Session.GetString("JWToken");
            return token;
        }

        [HttpPost] // <-- bỏ ("Create")
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewDTOs model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ToastError"] = "Vui lòng chọn ít nhất 1 sao và điền thông tin hợp lệ.";
                return RedirectToAction("HistoryBookingService", "BookingService", new { area = "Customer" });
            }

            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var jwt = GetJwtToken();
            var ok = await _reviewServiceCustomer.CreateReviewAsync(accountId, model, jwt);

            if (!ok)
            {
                TempData["ToastError"] = "Tạo đánh giá thất bại. Kiểm tra lại dữ liệu hoặc trạng thái booking.";
                return RedirectToAction("HistoryBookingService", "BookingService", new { area = "Customer" });
            }

            TempData["ToastSuccess"] = "Đánh giá thành công!";

            // Fallback nếu ServiceId chưa có (tránh nhảy về Cart/Index)
            if (model.ServiceId == Guid.Empty)
                return RedirectToAction("HistoryBookingService", "BookingService", new { area = "Customer" });

            Console.WriteLine($"[MVC] ImageFiles.Count = {model.ImageFiles?.Count ?? 0}");
            foreach (var f in model.ImageFiles ?? Enumerable.Empty<IFormFile>())
                Console.WriteLine($"[MVC] {f.FileName} {f.Length} bytes");


            return RedirectToAction("ListHistoryBooking", "BookingService", new { area = "Customer", id = model.ServiceId });
        }


        // GET: /Customer/ReviewCustomer
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var jwt = GetJwtToken();
            var (success, data, message) = await _reviewServiceCustomer.GetMyReviewsAsync(accountId, jwt, ct);

            if (!success)
            {
                TempData["ToastError"] = string.IsNullOrWhiteSpace(message)
                    ? "Không lấy được danh sách đánh giá."
                    : message;
                // Trả về view với list rỗng để UI vẫn render bình thường
                return View(new List<ReviewListItemDto>());
            }

            // data đã có URL tuyệt đối cho avatar/thumbnail/ảnh review -> render thẳng
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditReviewDto model, CancellationToken ct)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Model invalid -> trả lại Index + tự mở modal với dữ liệu user vừa nhập
            if (!ModelState.IsValid)
            {
                var jwt = GetJwtToken();
                var (ok, data, msg) = await _reviewServiceCustomer.GetMyReviewsAsync(accountId, jwt, ct);

                // Cho JS biết review nào cần mở và dữ liệu nào cần prefill
                ViewBag.OpenModalId = model.ReviewId;
                ViewBag.OpenModalDto = new
                {
                    ReviewId = model.ReviewId,
                    Rating = model.Rating,
                    Comment = model.Comment ?? string.Empty,
                    RemoveImages = model.RemoveImages ?? new List<string>() // để JS click sẵn các ảnh bị xoá
                };

                if (!ok)
                    TempData["ToastError"] = string.IsNullOrWhiteSpace(msg) ? "Không lấy được danh sách đánh giá." : msg;

                return View("Index", data ?? new List<ReviewListItemDto>());
            }

            // Model hợp lệ -> gọi API Edit
            var jwtToken = GetJwtToken();
            var success = await _reviewServiceCustomer.EditReviewAsync(accountId, model, jwtToken, ct);

            if (!success)
            {
                TempData["ToastError"] = "Cập nhật đánh giá thất bại.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Toast"] = "Cập nhật đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }



        // ================== DELETE ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid reviewId, CancellationToken ct)
        {
            var accountId = GetAccountId();
            if (accountId == Guid.Empty)
            {
                TempData["ToastError"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var jwt = GetJwtToken();
            var ok = await _reviewServiceCustomer.DeleteReviewAsync(accountId, reviewId, jwt, ct);

            if (!ok)
            {
                TempData["ToastError"] = "Xoá đánh giá thất bại.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ToastSuccess"] = "Đã xoá đánh giá.";
            return RedirectToAction(nameof(Index));
        }

    }
}