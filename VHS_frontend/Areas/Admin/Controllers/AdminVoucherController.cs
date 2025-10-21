// Areas/Admin/Controllers/AdminVoucherController.cs
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Voucher;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminVoucherController : Controller
    {
        private readonly AdminVoucherService _svc;
        public AdminVoucherController(AdminVoucherService svc) => _svc = svc;

        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token)) _svc.SetBearerToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? keyword = null, bool? onlyActive = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var (items, total) = await _svc.GetListAsync(new AdminVoucherQuery
            {
                Keyword = keyword,
                OnlyActive = onlyActive,
                Page = page,
                PageSize = pageSize
            }, ct);

            ViewBag.Total = total;
            ViewBag.Active = items.Count(x => x.IsActive == true);
            ViewBag.Inactive = items.Count(x => x.IsActive != true);
            ViewBag.Expired = items.Count(x => x.IsExpired);
            ViewBag.Keyword = keyword ?? "";
            ViewBag.OnlyActive = onlyActive;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var dto = await _svc.GetAsync(id, ct);
            return dto == null ? NotFound(new { message = "Không tìm thấy voucher." }) : Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminVoucherEditDTO dto, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var created = await _svc.CreateAsync(dto, ct);
                return Created(nameof(Get), created);
            }
            catch (DuplicateCodeException dup)
            {
                return Conflict(new { message = dup.Message });
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update(Guid id, [FromBody] AdminVoucherEditDTO dto, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var updated = await _svc.UpdateAsync(id, dto, ct);
                return updated == null
                    ? NotFound(new { message = "Không tìm thấy voucher." })
                    : Ok(updated);
            }
            catch (DuplicateCodeException dup)
            {
                return Conflict(new { message = dup.Message });
            }
            catch (ApiBadRequestException br)
            {
                return BadRequest(new { message = br.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var ok = await _svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound(new { message = "Không tìm thấy voucher." });
        }
    }
}
