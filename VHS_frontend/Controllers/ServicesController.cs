using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using VHS_frontend.Models.ServiceDTOs;
using VHS_frontend.Services.Customer.Implementations;
using VHS_frontend.Services.Customer.Interfaces;

namespace VHS_frontend.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IServiceCustomerService _serviceCustomerService;

        public ServicesController(IServiceCustomerService serviceCustomerService)
        {
            _serviceCustomerService = serviceCustomerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
         int page = 1,
         string? search = null,
         string? category = null,
         string? sort = null)
        {
            const int pageSize = 20;

            var dtos = await _serviceCustomerService.GetAllServiceHomePageAsync();

            var categories = await _serviceCustomerService.GetAllAsync();
            ViewBag.Categories = categories;

            var all = dtos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(s =>
                    (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.Description) && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(category, out var categoryId))
                {
                    all = all.Where(s => s.CategoryId == categoryId);
                }
                else
                {
                    all = all.Where(s =>
                        !string.IsNullOrWhiteSpace(s.CategoryName) &&
                        s.CategoryName.Equals(category, StringComparison.OrdinalIgnoreCase));
                }
            }

            all = sort switch
            {
                "price-500" => all.Where(s => s.Price < 500_000),
                "price-500-1000" => all.Where(s => s.Price >= 500_000 && s.Price < 1_000_000),
                "price-1000-5000" => all.Where(s => s.Price >= 1_000_000 && s.Price <= 5_000_000),
                "price-5000" => all.Where(s => s.Price > 5_000_000),
                "stars" => all.OrderByDescending(s => s.AverageRating).ThenByDescending(s => s.TotalReviews),
                "newest" => all.OrderByDescending(s => s.CreatedAt ?? DateTime.MinValue),
                _ => all.OrderBy(s => s.Title) 
            };

            page = Math.Max(1, page);
            var totalFiltered = all.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)pageSize));

            var models = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Sort = sort;

            return View(models);
        }


        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            Console.WriteLine($"[ServicesController] Details requested: id={id}");
            ServiceDetailDTOs? dto = await _serviceCustomerService.GetServiceDetailAsync(id);
            if (dto is null)
            {
                Console.WriteLine($"[ServicesController] Service not found: id={id}. Redirecting to Index.");
                TempData["Error"] = "Không tìm thấy dịch vụ hoặc dịch vụ đã bị gỡ.";
                return RedirectToAction(nameof(Index));
            }

            var top = await _serviceCustomerService.GetTop05HighestRatedServicesAsync();
            ViewBag.TopRight = top?.Take(4) ?? Enumerable.Empty<ListServiceHomePageDTOs>();

            var all = await _serviceCustomerService.GetAllServiceHomePageAsync();
            ViewBag.RelatedServices = all?
                .Where(s => s != null && s.ServiceId != dto.ServiceId)
                .Take(40)
                .ToList()
                ?? new System.Collections.Generic.List<ListServiceHomePageDTOs>();

            return View(dto);
        }

    }
}
