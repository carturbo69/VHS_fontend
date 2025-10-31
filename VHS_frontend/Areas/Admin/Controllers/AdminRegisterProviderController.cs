using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Admin.Models.Tag;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminRegisterProviderController : Controller
    {
        private readonly AdminRegisterProviderService _svc;
        private readonly CategoryAdminService _catSvc;
        private readonly TagAdminService _tagSvc;

        public AdminRegisterProviderController(
            AdminRegisterProviderService svc,
            CategoryAdminService catSvc,
            TagAdminService tagSvc)
        {
            _svc = svc;
            _catSvc = catSvc;
            _tagSvc = tagSvc;
        }

        private void AttachBearerIfAny()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrWhiteSpace(token))
                _svc.SetBearerToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = "All", CancellationToken ct = default)
        {
            // Mặc định = "All" (hiển thị tất cả)
            AttachBearerIfAny();

            // Backend service có thể hiểu "All" = không filter, nếu không thì bạn map ở service.
            var list = await _svc.GetListAsync(status, ct);

            ViewBag.Status = status; // để View set selected cho dropdown
            return View(list);       // Views/AdminRegisterProvider/Index.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            var dto = await _svc.GetDetailAsync(id, ct);
            if (dto == null)
                return RedirectToAction(nameof(Index));

            // ===== helper build absolute URL từ Request
            static string MakeAbs(HttpRequest req, string? u)
            {
                if (string.IsNullOrWhiteSpace(u)) return string.Empty;

                // đã absolute thì giữ nguyên
                if (Uri.TryCreate(u, UriKind.Absolute, out _)) return u;

                // ghép scheme://host[:port][/pathbase]/relative
                var scheme = req.Scheme;           // http/https
                var host = req.Host.ToUriComponent();
                var basePath = req.PathBase.HasValue ? req.PathBase.Value!.TrimEnd('/') : string.Empty;

                var relative = u.TrimStart('/');
                return $"{scheme}://{host}{basePath}/{relative}";
            }

            // 1) Avatar → absolute
            dto.Images = MakeAbs(Request, dto.Images);

            // 2) Certificate images (JSON string) → absolute rồi serialize lại
            foreach (var c in dto.Certificates)
            {
                if (string.IsNullOrWhiteSpace(c.Images))
                {
                    c.Images = "[]";
                    continue;
                }

                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(c.Images) ?? new();
                    var abs = list.Select(x => MakeAbs(Request, x)).ToList();
                    c.Images = JsonSerializer.Serialize(abs);
                }
                catch
                {
                    c.Images = "[]"; // JSON lỗi thì cho rỗng để view không vỡ
                }
            }

            // Map CategoryId -> Name
            var cats = await _catSvc.GetAllAsync(includeDeleted: false, ct);
            ViewBag.CatMap = cats.ToDictionary(x => x.CategoryId, x => x.Name);

            // Map TagId -> Name (load tất cả tags)
            var tags = await _tagSvc.GetAllAsync(includeDeleted: false, ct);
            ViewBag.TagMap = tags != null ? tags.ToDictionary(x => x.TagId, x => x.Name) : new Dictionary<Guid, string>();

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            var ok = await _svc.ApproveAsync(id, ct);
            TempData["AdminMsg"] = ok ? "Đã duyệt hồ sơ." : "Duyệt thất bại.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            var ok = await _svc.RejectAsync(id, reason, ct);
            TempData["AdminMsg"] = ok ? "Đã từ chối hồ sơ." : "Từ chối thất bại.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpDelete]
        [Route("Admin/AdminRegisterProvider/DeleteCertificate/{id}")]
        public async Task<IActionResult> DeleteCertificate(Guid id, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            try
            {
                var ok = await _svc.DeleteCertificateAsync(id, ct);
                if (ok)
                {
                    return Ok(new { message = "Đã xóa chứng chỉ thành công" });
                }
                else
                {
                    return BadRequest("Không thể xóa chứng chỉ");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpDelete]
        [Route("Admin/AdminRegisterProvider/RemoveTag/{id}/{tagId}")]
        public async Task<IActionResult> RemoveTag(Guid id, Guid tagId, CancellationToken ct = default)
        {
            AttachBearerIfAny();

            try
            {
                var ok = await _svc.RemoveTagFromCertificateAsync(id, tagId, ct);
                if (ok)
                {
                    return Ok(new { message = "Đã bỏ dịch vụ thành công" });
                }
                else
                {
                    return BadRequest("Không thể bỏ dịch vụ");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
    }
}
