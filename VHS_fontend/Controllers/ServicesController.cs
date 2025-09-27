using Microsoft.AspNetCore.Mvc;
using System;

namespace VHS_fontend.Controllers
{
    public class ServicesController : Controller
    {
        // Chỉ 1 action Index, có tham số mặc định
        [HttpGet]
        public IActionResult Index(int page = 1)
        {
            int pageSize = 20;      // mỗi trang 20 dịch vụ
            int totalServices = 100; // ví dụ
            int totalPages = (int)Math.Ceiling(totalServices / (double)pageSize);

            var services = Enumerable.Range(1, totalServices)
                .Select(i => new ServiceViewModel
                {
                    Id = i,
                    Name = $"Dịch vụ #{i}",
                    Description = "Mô tả ngắn gọn về dịch vụ này.",
                    Duration = $"{1 + (i % 4)}–{2 + (i % 6)} giờ",
                    Price = $"{100000 + i * 1000:n0}đ",
                    Stars = (i % 5) + 1,
                    ImageUrl = "/images/services/cleaning.jpg"
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(services);
        }
    }

    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Price { get; set; } = "";
        public int Stars { get; set; }
        public string ImageUrl { get; set; } = "";
    }
}
