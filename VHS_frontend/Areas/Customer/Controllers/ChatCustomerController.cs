using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using VHS_frontend.Areas.Customer.Models.ChatDemo;

namespace VHS_frontend.Areas.Customer.Controllers
{
    // Lưu mock data với ID cố định để không bị đổi sau mỗi request
    internal static class ChatMockStore
    {
        // Conversation IDs cố định
        public static readonly Guid Conv1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Conv2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid Conv3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // + NEW: hội thoại admin
        public static readonly Guid ConvAdminId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        // + NEW: tài khoản Admin
        public static readonly AccountDto Admin = new()
        {
            AccountId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            AccountName = "Admin",
            Email = "admin@example.com",
            Role = "Admin",
            AvatarUrl = "/images/admin.png" // bạn có thể đổi ảnh
        };

        // Accounts cố định
        public static readonly AccountDto Me = new()
        {
            AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            AccountName = "Bạn",
            Email = "me@example.com",
            Role = "User",
            AvatarUrl = "/images/sample1.png"
        };

        public static readonly AccountDto Shop1 = new()
        {
            AccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            AccountName = "ahgech3.vn",
            Email = "shop1@example.com",
            Role = "Provider",
            AvatarUrl = "https://placehold.co/72x72?text=S1"
        };

        public static readonly AccountDto Shop2 = new()
        {
            AccountId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            AccountName = "TiYin Studio Glasses",
            Email = "shop2@example.com",
            Role = "Provider",
            AvatarUrl = "/images/sample1.png"
        };

        public static readonly AccountDto Shop3 = new()
        {
            AccountId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            AccountName = "SofaHome",
            Email = "shop3@example.com",
            Role = "Provider",
            AvatarUrl = "/images/sample1.png"
        };
    }

    [Area("Customer")]
    public class ChatCustomerController : Controller
    {
        // ====== Helpers ======
        // Tạo receipt cho người nhận
        private static MessageReceiptDto Rcpt(Guid msgId, AccountDto to, DateTime? deliveredAt, DateTime? readAt)
            => new MessageReceiptDto
            {
                MessageId = msgId,
                RecipientAccountId = to.AccountId,
                Recipient = to,
                IsDelivered = deliveredAt != null,
                DeliveredAt = deliveredAt,
                IsRead = readAt != null,
                ReadAt = readAt
            };

        // Đếm số tin chưa đọc dành cho "myId"
        private static int UnreadFor(Guid myId, ConversationDto conv)
            => conv.Messages.Count(m =>
                   m.SenderAccountId != myId &&
                   m.Receipts.Any(r => r.RecipientAccountId == myId && !r.IsRead));

        // GET: /Customer/ChatCustomer
        public IActionResult Index(Guid? id)
        {
            // Lấy accounts cố định
            var me = ChatMockStore.Me;
            var admin = ChatMockStore.Admin;
            var shop1 = ChatMockStore.Shop1;
            var shop2 = ChatMockStore.Shop2;
            var shop3 = ChatMockStore.Shop3;

            // ======================= Conversation 1 (ahgech3.vn) =======================
            var conv1Id = ChatMockStore.Conv1Id;

            // m1: shop -> mình (mình ĐÃ xem)
            var m1 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv1Id,
                SenderAccountId = shop1.AccountId,
                Sender = shop1,
                Body = "Hi there! Thanks for your interest! Want to know more about this product?",
                CreatedAt = DateTime.Now.AddMinutes(-35)
            };
            m1.Receipts.Add(Rcpt(m1.MessageId, me, DateTime.Now.AddMinutes(-34), DateTime.Now.AddMinutes(-33)));

            // m2: mình -> shop (shop đã giao nhưng CHƯA xem)
            var m2 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv1Id,
                SenderAccountId = me.AccountId,
                Sender = me,
                Body = "Mình muốn hỏi thêm về chất liệu và bảo hành.",
                CreatedAt = DateTime.Now.AddMinutes(-30)
            };
            m2.Receipts.Add(Rcpt(m2.MessageId, shop1, DateTime.Now.AddMinutes(-29), null));

            // m3: shop -> mình (mình CHƯA xem)
            var m3 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv1Id,
                SenderAccountId = shop1.AccountId,
                Sender = shop1,
                Body = "Sản phẩm khung kim loại, bảo hành 12 tháng bạn nhé!",
                CreatedAt = DateTime.Now.AddMinutes(-28)
            };
            m3.Receipts.Add(Rcpt(m3.MessageId, me, DateTime.Now.AddMinutes(-27), null));

            // m4: shop -> mình (ảnh, mình CHƯA xem)
            var m4 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv1Id,
                SenderAccountId = shop1.AccountId,
                Sender = shop1,
                Body = "Đây là hình thực tế của sản phẩm:",
                MessageType = "TextWithImage",
                ImageUrl = "/images/sample1.png",
                CreatedAt = DateTime.Now.AddMinutes(-25)
            };
            m4.Receipts.Add(Rcpt(m4.MessageId, me, DateTime.Now.AddMinutes(-24), null));

            var conv1Messages = new List<MessageDto> { m1, m2, m3, m4 };

            var conv1 = new ConversationDto
            {
                ConversationId = conv1Id,
                Type = "UserProvider",
                CreatedAt = DateTime.Now.AddDays(-1),
                LastMessageAt = conv1Messages.Max(m => m.CreatedAt),
                Participants = new()
                {
                    new ConversationParticipantDto
                    {
                        ConversationId = conv1Id,
                        AccountId = shop1.AccountId,
                        RoleInConversation = "Provider",
                        JoinedAt = DateTime.Now.AddDays(-1),
                        Account = shop1
                    },
                    new ConversationParticipantDto
                    {
                        ConversationId = conv1Id,
                        AccountId = me.AccountId,
                        RoleInConversation = "User",
                        JoinedAt = DateTime.Now.AddDays(-1),
                        Account = me
                    }
                },
                Messages = conv1Messages
            };

            // ======================= Conversation 2 (TiYin Studio) =======================
            var conv2Id = ChatMockStore.Conv2Id;

            // n1: shop -> mình (mình ĐÃ xem)
            var n1 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv2Id,
                SenderAccountId = shop2.AccountId,
                Sender = shop2,
                Body = "Hello bạn! Mẫu này hiện còn size S và M nhé.",
                CreatedAt = DateTime.Now.AddMinutes(-80)
            };
            n1.Receipts.Add(Rcpt(n1.MessageId, me, DateTime.Now.AddMinutes(-79), DateTime.Now.AddMinutes(-78)));

            // n2: mình -> shop (shop ĐÃ xem)
            var n2 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv2Id,
                SenderAccountId = me.AccountId,
                Sender = me,
                Body = "Cho mình xem thêm ảnh thực tế được không?",
                CreatedAt = DateTime.Now.AddMinutes(-75)
            };
            n2.Receipts.Add(Rcpt(n2.MessageId, shop2, DateTime.Now.AddMinutes(-74), DateTime.Now.AddMinutes(-73)));

            // n3: shop -> mình (ảnh, mình ĐÃ xem)
            var n3 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv2Id,
                SenderAccountId = shop2.AccountId,
                Sender = shop2,
                MessageType = "Image",
                ImageUrl = "/images/sample1.png",
                CreatedAt = DateTime.Now.AddMinutes(-72)
            };
            n3.Receipts.Add(Rcpt(n3.MessageId, me, DateTime.Now.AddMinutes(-71), DateTime.Now.AddMinutes(-70)));

            // n4: mình -> shop (shop chỉ ĐÃ GIAO, chưa xem)
            var n4 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv2Id,
                SenderAccountId = me.AccountId,
                Sender = me,
                Body = "Đẹp quá! Mình sẽ đặt size M nha.",
                CreatedAt = DateTime.Now.AddMinutes(-70)
            };
            n4.Receipts.Add(Rcpt(n4.MessageId, shop2, DateTime.Now.AddMinutes(-69), null));

            var conv2Messages = new List<MessageDto> { n1, n2, n3, n4 };

            var conv2 = new ConversationDto
            {
                ConversationId = conv2Id,
                Type = "UserProvider",
                CreatedAt = DateTime.Now.AddDays(-2),
                LastMessageAt = conv2Messages.Max(m => m.CreatedAt),
                Participants = new()
                {
                    new ConversationParticipantDto
                    {
                        ConversationId = conv2Id,
                        AccountId = shop2.AccountId,
                        RoleInConversation = "Provider",
                        JoinedAt = DateTime.Now.AddDays(-2),
                        Account = shop2
                    },
                    new ConversationParticipantDto
                    {
                        ConversationId = conv2Id,
                        AccountId = me.AccountId,
                        RoleInConversation = "User",
                        JoinedAt = DateTime.Now.AddDays(-2),
                        Account = me
                    }
                },
                Messages = conv2Messages
            };

            // ======================= Conversation 3 (SofaHome) =======================
            var conv3Id = ChatMockStore.Conv3Id;

            // p1: shop -> mình (mình ĐÃ xem)
            var p1 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv3Id,
                SenderAccountId = shop3.AccountId,
                Sender = shop3,
                Body = "Mẫu sofa này đang giảm giá 20%.",
                CreatedAt = DateTime.Now.AddMinutes(-50)
            };
            p1.Receipts.Add(Rcpt(p1.MessageId, me, DateTime.Now.AddMinutes(-49), DateTime.Now.AddMinutes(-48)));

            // p2: shop -> mình (ảnh, mình CHƯA xem)
            var p2 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv3Id,
                SenderAccountId = shop3.AccountId,
                Sender = shop3,
                MessageType = "TextWithImage",
                Body = "Đây là hình thực tế tại showroom.",
                ImageUrl = "/images/sample1.png",
                CreatedAt = DateTime.Now.AddMinutes(-48)
            };
            p2.Receipts.Add(Rcpt(p2.MessageId, me, DateTime.Now.AddMinutes(-47), null));

            // p3: mình -> shop (shop ĐÃ xem)
            var p3 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conv3Id,
                SenderAccountId = me.AccountId,
                Sender = me,
                Body = "Nhìn đẹp quá! Có giao ở HN không?",
                CreatedAt = DateTime.Now.AddMinutes(-45)
            };
            p3.Receipts.Add(Rcpt(p3.MessageId, shop3, DateTime.Now.AddMinutes(-44), DateTime.Now.AddMinutes(-43)));

            var conv3Messages = new List<MessageDto> { p1, p2, p3 };

            var conv3 = new ConversationDto
            {
                ConversationId = conv3Id,
                Type = "UserProvider",
                CreatedAt = DateTime.Now.AddDays(-3),
                LastMessageAt = conv3Messages.Max(m => m.CreatedAt),
                Participants = new()
                {
                    new ConversationParticipantDto
                    {
                        ConversationId = conv3Id,
                        AccountId = shop3.AccountId,
                        RoleInConversation = "Provider",
                        JoinedAt = DateTime.Now.AddDays(-3),
                        Account = shop3
                    },
                    new ConversationParticipantDto
                    {
                        ConversationId = conv3Id,
                        AccountId = me.AccountId,
                        RoleInConversation = "User",
                        JoinedAt = DateTime.Now.AddDays(-3),
                        Account = me
                    }
                },
                Messages = conv3Messages
            };

            // ======================= Conversation Admin (UserAdmin) =======================
            var convAdminId = ChatMockStore.ConvAdminId;

            // a1: mình -> admin (admin đã giao & đã xem)
            var a1 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = convAdminId,
                SenderAccountId = me.AccountId,
                Sender = me,
                Body = "Chào admin, giúp mình kiểm tra đơn #DH-1024 với.",
                CreatedAt = DateTime.Now.AddMinutes(-20)
            };
            a1.Receipts.Add(Rcpt(a1.MessageId, admin, DateTime.Now.AddMinutes(-19), DateTime.Now.AddMinutes(-18)));

            // a2: admin -> mình (mình ĐÃ xem)
            var a2 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = convAdminId,
                SenderAccountId = admin.AccountId,
                Sender = admin,
                Body = "Mình đã kiểm tra: đơn đang ở trạng thái 'Đang giao'. Dự kiến giao hôm nay.",
                CreatedAt = DateTime.Now.AddMinutes(-17)
            };
            a2.Receipts.Add(Rcpt(a2.MessageId, me, DateTime.Now.AddMinutes(-16), DateTime.Now.AddMinutes(-15)));

            // a3: mình -> admin (admin đã giao nhưng CHƯA xem)
            var a3 = new MessageDto
            {
                MessageId = Guid.NewGuid(),
                ConversationId = convAdminId,
                SenderAccountId = me.AccountId,
                Sender = me,
                Body = "Cảm ơn admin! Có thể cập nhật số điện thoại nhận hàng giúp mình không?",
                CreatedAt = DateTime.Now.AddMinutes(-14)
            };
            a3.Receipts.Add(Rcpt(a3.MessageId, admin, DateTime.Now.AddMinutes(-13), null));

            var convAdminMessages = new List<MessageDto> { a1, a2, a3 };

            var convAdmin = new ConversationDto
            {
                ConversationId = convAdminId,
                Type = "UserAdmin",
                CreatedAt = DateTime.Now.AddDays(-1),
                LastMessageAt = convAdminMessages.Max(m => m.CreatedAt),
                Participants = new()
                {
                    new ConversationParticipantDto
                    {
                        ConversationId = convAdminId,
                        AccountId = admin.AccountId,
                        RoleInConversation = "Admin",
                        JoinedAt = DateTime.Now.AddDays(-1),
                        Account = admin
                    },
                    new ConversationParticipantDto
                    {
                        ConversationId = convAdminId,
                        AccountId = me.AccountId,
                        RoleInConversation = "User",
                        JoinedAt = DateTime.Now.AddDays(-1),
                        Account = me
                    }
                },
                Messages = convAdminMessages
            };

            // ===== Helper tính điều kiện pin cho sidebar (auto pin khi user đã từng nhắn trong conv Admin)
            Func<ConversationDto, bool> isPinned = conv =>
                conv.Type == "UserAdmin" && conv.Messages.Any(m => m.SenderAccountId == me.AccountId);

            // ======================= Sidebar list (Unread tự tính) =======================
            var conversationsSidebar = new List<ConversationListItemVm>
            {
                new()
                {
                    ConversationId = conv1Id,
                    Title = shop1.AccountName,
                    AvatarUrl = shop1.AvatarUrl,
                    LastMessageSnippet = "Đây là hình thực tế của sản phẩm:",
                    LastMessageAt = conv1.LastMessageAt,
                    UnreadCount = UnreadFor(me.AccountId, conv1),
                    IsOnline = true,
                    IsPinned = isPinned(conv1)
                },
                new()
                {
                    ConversationId = conv2Id,
                    Title = shop2.AccountName,
                    AvatarUrl = shop2.AvatarUrl,
                    LastMessageSnippet = "Đẹp quá! Mình sẽ đặt size M nha.",
                    LastMessageAt = conv2.LastMessageAt,
                    UnreadCount = UnreadFor(me.AccountId, conv2),
                    IsOnline = false,
                    IsPinned = isPinned(conv2)
                },
                new()
                {
                    ConversationId = conv3Id,
                    Title = shop3.AccountName,
                    AvatarUrl = shop3.AvatarUrl,
                    LastMessageSnippet = "Nhìn đẹp quá! Có giao ở HN không?",
                    LastMessageAt = conv3.LastMessageAt,
                    UnreadCount = UnreadFor(me.AccountId, conv3),
                    IsOnline = true,
                    IsPinned = isPinned(conv3)
                },
                // + NEW: Admin (User-Admin)
                new()
                {
                    ConversationId = convAdminId,
                    Title = admin.AccountName,
                    AvatarUrl = admin.AvatarUrl,
                    LastMessageSnippet = "Cảm ơn admin! Có thể cập nhật số điện thoại nhận hàng giúp mình không?",
                    LastMessageAt = convAdmin.LastMessageAt,
                    UnreadCount = UnreadFor(me.AccountId, convAdmin),
                    IsOnline = true,
                    IsPinned = isPinned(convAdmin)
                }
            };

            // ======================= Chọn hội thoại theo id =======================
            var map = new Dictionary<Guid, ConversationDto>
            {
                [conv1Id] = conv1,
                [conv2Id] = conv2,
                [conv3Id] = conv3,
                [convAdminId] = convAdmin
            };

            // Mặc định mở hội thoại Admin (có thể đổi lại conv1Id nếu muốn như trước)
            var selectedId = (id.HasValue && map.ContainsKey(id.Value)) ? id.Value : convAdminId;

            var vm = new ChatPageVm
            {
                CurrentAccount = me,
                Conversations = conversationsSidebar,
                SelectedConversationId = selectedId,
                SelectedConversation = map[selectedId],
                ProductCard = new ProductCardVm
                {
                    Title = "Kính mắt gọng tròn kim loại phong cách",
                    ImageUrl = "/images/sample1.png",
                    Badge = "Đã hỏi",
                    ItemsCount = 832,
                    PriceRangeText = "27.000đ - 45.360đ",
                    BuyNowUrl = "#"
                },
                SafetyTips = new List<TipItemVm>
                {
                    new() { Text = "Shopee KHÔNG cho phép đặt cọc/chuyển khoản riêng…", LinkText = "Tìm hiểu thêm", LinkUrl = "#" }
                }
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Send(Guid conversationId, string body, IFormFile? image)
        {
            // Demo: lưu ảnh vào wwwroot/uploads và set ImageUrl để hiển thị
            string? imageUrl = null;

            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploads, fileName);
                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(fs);
                }
                imageUrl = $"/uploads/{fileName}";
            }

            // TODO: ở dự án thực tế, bạn sẽ tạo Message mới và lưu vào DB.
            // Ở mock này, chỉ redirect để refresh UI theo conversationId.
            return RedirectToAction("Index", new { id = conversationId });
        }
    }
}
