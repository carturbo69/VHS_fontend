using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Category;
using VHS_frontend.Areas.Admin.Models.Tag;
using VHS_frontend.Areas.Admin.Models.Option;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly CategoryAdminService _svc;
        private readonly TagAdminService _tagSvc;
        private readonly OptionAdminService _optionSvc;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public CategoryController(CategoryAdminService svc, TagAdminService tagSvc, OptionAdminService optionSvc)
        {
            _svc = svc;
            _tagSvc = tagSvc;
            _optionSvc = optionSvc;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool includeDeleted = false)
        {
            var list = await _svc.GetAllAsync(includeDeleted);
            ViewData["IncludeDeleted"] = includeDeleted;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var dto = await _svc.GetByIdAsync(id);
            if (dto is null) return NotFound();
            return Json(dto, _json);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDTO dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name)) return BadRequest("Name is required.");
            var res = await _svc.CreateAsync(dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPut]
        public async Task<IActionResult> Update(Guid id, [FromBody] CategoryUpdateDTO dto, CancellationToken ct)
        {
            var res = await _svc.UpdateAsync(id, dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var res = await _svc.DeleteAsync(id, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            var res = await _svc.RestoreAsync(id, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }


        // ===== TAG proxies dùng chung trang Category =====
        [HttpGet]
        public async Task<IActionResult> GetTags(Guid categoryId, bool includeDeleted = false, CancellationToken ct = default)
        {
            var list = await _tagSvc.GetByCategoryAsync(categoryId, includeDeleted, ct) ?? new();
            return Json(list, _json);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagCreateDTO dto, CancellationToken ct)
        {
            if (dto == null || dto.CategoryId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("CategoryId & Name are required.");

            var res = await _tagSvc.CreateAsync(dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTag(Guid id, [FromBody] TagUpdateDTO dto, CancellationToken ct)
        {
            var res = await _tagSvc.UpdateAsync(id, dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTag(Guid id, CancellationToken ct)
        {
            var res = await _tagSvc.DeleteAsync(id, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreTag(Guid id, CancellationToken ct)
        {
            var res = await _tagSvc.RestoreAsync(id, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        // ===== OPTION proxies cho Tag =====
        [HttpGet]
        public async Task<IActionResult> GetOptions(Guid tagId, CancellationToken ct = default)
        {
            var list = await _optionSvc.GetByTagAsync(tagId, ct) ?? new();
            return Json(list, _json);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOption([FromBody] OptionCreateDTO dto, CancellationToken ct)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.OptionName) || string.IsNullOrWhiteSpace(dto.Type))
                return BadRequest("OptionName & Type are required.");

            var res = await _optionSvc.CreateAsync(dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateOption(Guid id, [FromBody] OptionUpdateDTO dto, CancellationToken ct)
        {
            var res = await _optionSvc.UpdateAsync(id, dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteOption(Guid id, CancellationToken ct)
        {
            var res = await _optionSvc.DeleteAsync(id, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }
    }
}
