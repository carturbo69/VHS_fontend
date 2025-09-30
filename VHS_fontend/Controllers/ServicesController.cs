using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;

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
            var rnd = new Random(42); // dùng cho TotalReviews

            // Fake data
            var all = Enumerable.Range(1, totalServices)
                .Select(i =>
                {
                    var cat = (i % 3) switch
                    {
                        0 => "Vệ sinh",
                        1 => "Nội trợ",
                        _ => "Sự kiện nhỏ tại nhà"
                    };
                    var price = i * 75_000; // 75k..7.5tr

                    // ⭐ Sao: seed theo id để List/Detail giống nhau, clamp 1..5
                    var starRand = new Random(i * 99991 + 7);
                    var starsRaw = starRand.NextDouble() * 4.0 + 1.0;   // 1.0..5.0
                    var stars = Math.Round(Math.Min(5.0, Math.Max(1.0, starsRaw)), 2);

                    return new ServiceViewModel
                    {
                        Id = i,
                        Name = $"{cat} #{i}",
                        Description = "Mô tả ngắn gọn về dịch vụ này. Chuyên nghiệp – nhanh – gọn.",
                        Duration = $"{1 + (i % 3)}–{2 + (i % 5)} giờ",
                        PriceValue = price,
                        PriceDisplay = $"{price:n0}đ",
                        Stars = stars,
                        TotalReviews = rnd.Next(10, 500),
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

            // Sort / Price range filter / Rating
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

        [HttpGet]
        public IActionResult Details(int id)
        {
            // ⭐ Sao: seed theo id để đồng bộ với List
            var starRandMain = new Random(id * 99991 + 7);
            var starsRawMain = starRandMain.NextDouble() * 4.0 + 1.0;   // 1..5
            var starsMain = Math.Round(Math.Min(5.0, Math.Max(1.0, starsRawMain)), 2);

            var categories = new[] { "Vệ sinh", "Nội trợ", "Sự kiện nhỏ tại nhà" };
            var cat = categories[Math.Abs(id) % categories.Length];

            var price = Math.Max(150_000, (Math.Abs(id) % 100 + 1) * 80_000); // 80k..8tr
            var model = new ServiceDetailViewModel
            {
                Id = id,
                Name = $"{cat} #{id}",
                Description = "Mô tả chi tiết dịch vụ. Đội ngũ chuyên nghiệp, quy trình chuẩn, nhanh gọn, sạch sẽ. Bao gồm vật tư cơ bản và bảo hành theo điều khoản áp dụng.",
                Duration = $"{1 + (Math.Abs(id) % 3)}–{2 + (Math.Abs(id) % 5)} giờ",
                PriceValue = price,
                PriceDisplay = $"{price:n0}đ",
                Stars = starsMain,
                TotalReviews = new Random(id * 12347 + 11).Next(20, 800),
                Category = cat,
                Images = new[]
                {
                    Url.Content("~/images/VeSinh.jpg"),
                    Url.Content("~/images/VeSinh.jpg"),
                    Url.Content("~/images/VeSinh.jpg"),
                },
                // ---- Add-ons mô phỏng ----
                AddOns = new List<AddOnItem>
                {
                    new AddOnItem{ Id=1, Name="Vệ sinh thêm phòng", Price=50_000, Type="qty", MaxQty=5 },
                    new AddOnItem{ Id=2, Name="Dụng cụ chuyên dụng", Price=30_000, Type="check" },
                    new AddOnItem{ Id=3, Name="Khử khuẩn nano", Price=70_000, Type="check" },
                    new AddOnItem{ Id=4, Name="Tăng ca (mỗi giờ)", Price=100_000, Type="qty", MaxQty=4 },
                }
            };

            // Related (7 mục)
            var related = Enumerable.Range(1, 7).Select(i =>
            {
                var rid = (Math.Abs(id) + i) % 100 + 1;
                var rcat = categories[rid % categories.Length];
                var rprice = Math.Max(150_000, (rid % 100 + 1) * 70_000);

                var starRand = new Random(rid * 99991 + 7);
                var starsRaw = starRand.NextDouble() * 4.0 + 1.0;
                var stars = Math.Round(Math.Min(5.0, Math.Max(1.0, starsRaw)), 2);

                return new ServiceViewModel
                {
                    Id = rid,
                    Name = $"{rcat} #{rid}",
                    Description = "Mô tả ngắn gọn...",
                    Duration = $"{1 + (rid % 3)}–{2 + (rid % 5)} giờ",
                    PriceValue = rprice,
                    PriceDisplay = $"{rprice:n0}đ",
                    Stars = stars,
                    TotalReviews = 50 + rid,
                    Category = rcat,
                    ImageUrl = Url.Content("~/images/VeSinh.jpg")
                };
            }).ToList();

            model.Related = related;

            // ----- Mock Reviews (6..14 để có phân trang 5/trang) -----
            var rr = new Random(id * 777 + 3);
            int reviewCount = rr.Next(6, 15);
            var comments = new[]
            {
                "Dịch vụ nhanh, gọn, sạch sẽ. Sẽ ủng hộ tiếp!",
                "Nhân viên thân thiện, làm việc cẩn thận.",
                "Ổn so với giá tiền, đặt lịch linh hoạt.",
                "Đúng mô tả, hoàn thành đúng giờ.",
                "Có thể cải thiện thêm về dụng cụ chuyên dụng."
            };
            var names = new[] { "Minh", "Lan", "Huy", "Ngọc", "Tú", "Thảo", "Khoa", "Dương" };

            var reviews = new List<ReviewItem>();
            for (int i = 0; i < reviewCount; i++)
            {
                var starsRaw = Math.Min(5.0, Math.Max(1.0, 3.0 + rr.NextDouble() * 2.0)); // 3.0..5.0
                var photoCount = rr.NextDouble() < 0.35 ? rr.Next(1, 4) : 0;
                var photos = Enumerable.Repeat(Url.Content("~/images/VeSinh.jpg"), photoCount).ToArray();

                reviews.Add(new ReviewItem
                {
                    Id = i + 1,
                    User = names[rr.Next(names.Length)] + rr.Next(10, 99),
                    AvatarUrl = Url.Content("~/images/VeSinh.jpg"),
                    Stars = Math.Round(starsRaw, 1),
                    Content = comments[rr.Next(comments.Length)],
                    Variant = $"{1 + (i % 3)} phòng, {1 + (i % 2)} nhân sự",
                    Photos = photos,
                    CreatedAt = DateTime.Today.AddDays(-rr.Next(1, 400)).AddMinutes(rr.Next(0, 1440)),
                    Likes = rr.Next(0, 20)
                });
            }
            model.Reviews = reviews;
            model.TotalReviews = reviews.Count; // đồng bộ tổng đánh giá

            return View(model);
        }
    }

    // ============================ ViewModels ================================
    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Duration { get; set; } = "";
        public int PriceValue { get; set; }                 // VND (số thuần để tính)
        public string PriceDisplay { get; set; } = "";      // "1.200.000đ"
        public double Stars { get; set; }                   // ⭐ 1.00..5.00
        public int TotalReviews { get; set; }
        public string Category { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    public class ServiceDetailViewModel : ServiceViewModel
    {
        public string[] Images { get; set; } = Array.Empty<string>();
        public List<ServiceViewModel> Related { get; set; } = new();
        public List<AddOnItem> AddOns { get; set; } = new();          // Add-ons
        public List<ReviewItem> Reviews { get; set; } = new();        // Reviews
    }

    public class AddOnItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Price { get; set; }            // VND
        public string Unit { get; set; } = "đ";
        public string Type { get; set; } = "qty"; // "check" | "qty"
        public int MaxQty { get; set; } = 10;
    }

    public class ReviewItem
    {
        public int Id { get; set; }
        public string User { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public double Stars { get; set; }      // 1.0..5.0 (có lẻ)
        public string Content { get; set; } = "";
        public string Variant { get; set; } = "";
        public string[] Photos { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
        public int Likes { get; set; }
    }
}
