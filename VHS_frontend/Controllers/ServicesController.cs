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

            // 1) Lấy dữ liệu thật từ API (List<ListServiceHomePageDTOs>)
            var dtos = await _serviceCustomerService.GetAllServiceHomePageAsync();

            // 2) IQueryable để chain filter/sort
            var all = dtos.AsQueryable();

            // 3) Search theo Title/Description
            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(s =>
                    (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.Description) && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            //// 4) Category filter: map từ UnitType => 3 nhóm cố định
            //if (!string.IsNullOrWhiteSpace(category))
            //{
            //    all = all.Where(s => MapCategory);
            //}

            // 5) Sort / khoảng giá / rating
            all = sort switch
            {
                "price-500" => all.Where(s => s.Price < 500_000),
                "price-500-1000" => all.Where(s => s.Price >= 500_000 && s.Price < 1_000_000),
                "price-1000-5000" => all.Where(s => s.Price >= 1_000_000 && s.Price <= 5_000_000),
                "price-5000" => all.Where(s => s.Price > 5_000_000),
                "stars" => all.OrderByDescending(s => s.AverageRating),
                _ => all.OrderBy(s => s.Title) // hoặc CreatedAt tùy ý
            };

            // 6) Paging
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
        public async Task<IActionResult> Details(Guid id)
        {
            // 1) Chi tiết dịch vụ (DTO thật)
            ServiceDetailDTOs? dto = await _serviceCustomerService.GetServiceDetailAsync(id);
            if (dto is null) return NotFound();

            // 2) Top 4 dịch vụ nổi bật (để cột phải)
            var top = await _serviceCustomerService.GetTop05HighestRatedServicesAsync();
            ViewBag.TopRight = top?.Take(4) ?? Enumerable.Empty<ListServiceHomePageDTOs>();

            // 3) Related services (tùy chỉnh tiêu chí lọc theo nhu cầu)
            var all = await _serviceCustomerService.GetAllServiceHomePageAsync();
            ViewBag.RelatedServices = all?
                .Where(s => s != null && s.ServiceId != dto.ServiceId) // loại chính nó
                .Take(40)
                .ToList()
                ?? new System.Collections.Generic.List<ListServiceHomePageDTOs>();

            // 4) Trả về DTO cho View
            return View(dto);
        }

    }
}
