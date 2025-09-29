using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace VHS_fontend.Controllers
{
    public class ServicesController : Controller
    {
        [HttpGet]
        public IActionResult Index(
            int page = 1,
            string? search = null,
            string? category = null,
            string? sort = null
        )
        {
            const int pageSize = 20;
            const int totalServices = 100;
            var rnd = new Random();

            // Fake data
            var all = Enumerable.Range(1, totalServices)
                .Select(i =>
                {
                    var price = i * 75_000;
                    var cat = (i % 3) switch
                    {
                        0 => "Vệ sinh",
                        1 => "Sửa chữa",
                        _ => "Giúp việc"
                    };

                    return new ServiceViewModel
                    {
                        Id = i,
                        Name = $"{cat} #{i}",
                        Description = "Mô tả ngắn gọn về dịch vụ này. Chuyên nghiệp – nhanh – gọn.",
                        Duration = $"{1 + (i % 3)}–{2 + (i % 5)} giờ",
                        PriceValue = price,
                        PriceDisplay = $"{price:n0}đ",
                        Stars = Math.Round(rnd.NextDouble() * 4 + 1, 1), // 1.0 – 5.0
                        TotalReviews = rnd.Next(10, 500), // 10 – 500 đánh giá
                        Category = cat,
                        ImageUrl = Url.Content("~/images/VeSinh.jpg")
                    };
                })
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(s =>
                    s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Category
            if (!string.IsNullOrWhiteSpace(category))
            {
                all = all.Where(s => s.Category == category);
            }

            // Sort / Filter by price or stars
            all = sort switch
            {
                "price-500" => all.Where(s => s.PriceValue < 500_000),
                "price-500-1000" => all.Where(s => s.PriceValue >= 500_000 && s.PriceValue < 1_000_000),
                "price-1000-5000" => all.Where(s => s.PriceValue >= 1_000_000 && s.PriceValue <= 5_000_000),
                "price-5000" => all.Where(s => s.PriceValue > 5_000_000),
                "stars" => all.OrderByDescending(s => s.Stars),
                _ => all.OrderBy(s => s.Id)
            };

            var totalFiltered = all.Count();
            var totalPages = (int)Math.Ceiling(totalFiltered / (double)pageSize);

            var models = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, totalPages);
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Sort = sort;

            return View(models);
        }
    }

    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Duration { get; set; } = "";
        public int PriceValue { get; set; }
        public string PriceDisplay { get; set; } = "";
        public double Stars { get; set; } // trung bình (có thể 2.5)
        public int TotalReviews { get; set; }
        public string Category { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }
}
