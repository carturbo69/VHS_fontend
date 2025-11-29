using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VHS_frontend.Areas.Admin.Models.ChatDTOs;
using VHS_frontend.Services.Admin;

namespace VHS_frontend.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChatAdminController : Controller
    {
        private readonly ChatAdminService _chatService;

        public ChatAdminController(ChatAdminService chatService)
        {
            _chatService = chatService;
        }

        private Guid GetAccountId()
        {
            // Ưu tiên claim "AccountID", fallback Session
            var idStr = User.FindFirstValue("AccountID") ?? HttpContext.Session.GetString("AccountID");
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }

        private string? GetJwtFromRequest()
        {
            if (Request.Cookies.TryGetValue("jwt", out var jwt) && !string.IsNullOrWhiteSpace(jwt))
                return jwt;

            var s = HttpContext.Session.GetString("jwt");
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        /// <summary>
        /// Nếu chưa có AccountID thì redirect về /Account/Login?returnUrl=...
        /// Dùng cho mọi action, đỡ lặp code.
        /// </summary>
        private IActionResult? RedirectIfNoAccountId(out Guid myAccountId)
        {
            myAccountId = GetAccountId();
            if (myAccountId != Guid.Empty) return null;

            var returnUrl = $"{Request.Path}{Request.QueryString}";
            return RedirectToAction("Login", "Account", new { area = "", returnUrl });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadTotal(CancellationToken ct)
        {
            if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

            var jwt = GetJwtFromRequest();

            var total = await _chatService.GetUnreadTotalAsync(
                accountId: myId,
                jwtToken: jwt,
                ct: ct
            );

            return Json(new { total });
        }

        [HttpGet]
        public async Task<IActionResult> WithProvider(Guid providerId, CancellationToken ct)
        {
            if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

            var jwt = GetJwtFromRequest();

            var conversationId = await _chatService.FindOrStartConversationByProviderAsync(
                myAccountId: myId,
                providerId: providerId,
                jwtToken: jwt,
                ct: ct
            );

            return RedirectToAction(nameof(Index), new { id = conversationId });
        }

        public async Task<IActionResult> Index(Guid? id, CancellationToken ct)
        {
            if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

            var jwt = GetJwtFromRequest();

            // 1) Lấy danh sách hội thoại (sidebar)
            var conversations = await _chatService.GetConversationsAsync(myId, jwtToken: jwt, ct: ct);

            // 2) Chỉ chọn khi user click vào id hợp lệ
            Guid? selectedId = null;
            if (id.HasValue && conversations.Any(c => c.ConversationId == id.Value))
            {
                selectedId = id.Value;
            }

            ConversationDto? selectedConv = null;

            // 3) Chỉ gọi API chi tiết khi đã chọn
            if (selectedId.HasValue)
            {
                selectedConv = await _chatService.GetConversationDetailAsync(
                    conversationId: selectedId.Value,
                    accountId: myId,
                    take: 50,
                    before: null,
                    markAsRead: true,
                    jwtToken: jwt,
                    ct: ct
                );
            }

            var vm = new ChatPageVm
            {
                CurrentAccount = new MessageAccountDto
                {
                    AccountId = myId,
                    AccountName = User.Identity?.Name ?? "Tôi"
                },
                Conversations = conversations,
                SelectedConversationId = selectedId,
                SelectedConversation = selectedConv,
                ProductCard = null,
                SafetyTips = new List<TipItemVm>()
            };

            return View(vm);
        }

        // Areas/Customer/Controllers/ChatCustomerController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversation(Guid id, bool hide = false, CancellationToken ct = default)
        {
            if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

            var jwt = GetJwtFromRequest();

            await _chatService.ClearForMeAsync(
                conversationId: id,
                accountId: myId,
                hide: hide,              //  giờ luôn nhận true từ form
                jwtToken: jwt,
                ct: ct
            );

            // Quay về trang Chat, KHÔNG chọn hội thoại nào
            return RedirectToAction(nameof(Index));
        }



        //   [HttpPost]
        //   [ValidateAntiForgeryToken]
        //   public async Task<IActionResult> Send(
        //Guid conversationId,
        //string? body,
        //IFormFile? image,
        //Guid? replyToMessageId,            //  thêm tham số này để nhận từ form
        //CancellationToken ct)
        //   {
        //       if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

        //       var jwt = GetJwtFromRequest();

        //       await _chatService.SendMessageAsync(
        //           conversationId: conversationId,
        //           accountId: myId,               // đổi tên tham số cho khớp service mới
        //           body: body,
        //           image: image,
        //           replyToMessageId: replyToMessageId, // truyền xuống backend
        //           jwtToken: jwt,
        //           ct: ct
        //       );

        //       return RedirectToAction(nameof(Index), new { id = conversationId });
        //   }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
 Guid conversationId,
 string? body,
 IFormFile? image,
 Guid? replyToMessageId,
 CancellationToken ct)
        {
            if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

            var jwt = GetJwtFromRequest();

            await _chatService.SendMessageAsync(
                conversationId: conversationId,
                accountId: myId,
                body: body,
                image: image,
                replyToMessageId: replyToMessageId,
                jwtToken: jwt,
                ct: ct
            );

            return RedirectToAction(nameof(Index), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(Guid conversationId, CancellationToken ct)
        {
            // Ép login nếu chưa có AccountID
            if (RedirectIfNoAccountId(out var myId) is IActionResult goLogin) return goLogin;

            var jwt = GetJwtFromRequest();

            await _chatService.MarkConversationReadAsync(
                conversationId: conversationId,
                accountId: myId,
                jwtToken: jwt,
                ct: ct
            );

            // Frontend chỉ cần 200 OK là đủ
            return Ok(new { success = true });
        }
    }
}
