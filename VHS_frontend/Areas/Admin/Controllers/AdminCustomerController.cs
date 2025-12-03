using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Customer;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminCustomerController : Controller
    {
        private readonly CustomerAdminService _svc;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public AdminCustomerController(CustomerAdminService svc) => _svc = svc;
        
        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token)) _svc.SetBearerToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool includeDeleted = false)
        {
            AttachBearerIfAny();
            var list = await _svc.GetAllAsync(includeDeleted);
            ViewData["IncludeDeleted"] = includeDeleted;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            AttachBearerIfAny();
            var dto = await _svc.GetByIdAsync(id);
            if (dto is null) return NotFound();
            return Json(dto, _json);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDTO dto, CancellationToken ct)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.AccountName)
                            || string.IsNullOrWhiteSpace(dto.Email)
                            || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("AccountName, Email, Password are required.");

            AttachBearerIfAny();
            dto.Role = "User"; // khoá role
            var res = await _svc.CreateAsync(dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPut]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDTO dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest("Body required.");
            AttachBearerIfAny();
            dto.Role = "User"; // khoá role

            var res = await _svc.UpdateAsync(id, dto, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, [FromBody] Dictionary<string, string>? body = null, CancellationToken ct = default)
        {
            AttachBearerIfAny();
            var lockReason = body?.GetValueOrDefault("LockReason");
            var res = await _svc.DeleteAsync(id, lockReason, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            AttachBearerIfAny();
            var res = await _svc.RestoreAsync(id, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            return StatusCode((int)res.StatusCode, text);
        }
    }
}
