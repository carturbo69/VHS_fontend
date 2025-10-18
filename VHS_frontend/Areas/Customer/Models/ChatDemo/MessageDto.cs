namespace VHS_frontend.Areas.Customer.Models.ChatDemo
{
    // DTO cơ bản (phản chiếu các Model bạn đã cung cấp)
    public class AccountDto
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? AvatarUrl { get; set; }   // phụ trợ UI
        public bool? IsDeleted { get; set; }
    }

    public class UserDto
    {
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Images { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsDeleted { get; set; }
        public AccountDto Account { get; set; } = null!;
    }

    public class ProviderDto
    {
        public Guid ProviderId { get; set; }
        public Guid AccountId { get; set; }
        public string ProviderName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? Images { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsDeleted { get; set; }
        public AccountDto Account { get; set; } = null!;
    }

    public class MessageReceiptDto
    {
        public Guid MessageId { get; set; }
        public Guid RecipientAccountId { get; set; }
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public AccountDto Recipient { get; set; } = null!;
    }

    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderAccountId { get; set; }

        public string? Body { get; set; }
        public string MessageType { get; set; } = "Text"; // "Text" | "Image" | "TextWithImage" | "System"
        public Guid? ReplyToMessageId { get; set; }
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? Metadata { get; set; }

        public AccountDto Sender { get; set; } = null!;
        public MessageDto? ReplyTo { get; set; }
        public List<MessageReceiptDto> Receipts { get; set; } = new();
    }

    public class ConversationParticipantDto
    {
        public Guid ConversationId { get; set; }
        public Guid AccountId { get; set; }
        public string RoleInConversation { get; set; } = null!;
        public DateTime JoinedAt { get; set; }

        public AccountDto Account { get; set; } = null!;
    }

    public class ConversationDto
    {
        public Guid ConversationId { get; set; }
        public string Type { get; set; } = null!; // "UserProvider" | "UserAdmin"
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public Guid CreatedByAccountId { get; set; }

        public List<ConversationParticipantDto> Participants { get; set; } = new();
        public List<MessageDto> Messages { get; set; } = new();
    }

    // ViewModel cho trang Chat (gom tất cả dữ liệu cần render)
    public class ChatPageVm
    {
        // Sidebar
        public List<ConversationListItemVm> Conversations { get; set; } = new();
        public Guid? SelectedConversationId { get; set; }

        // Khung chat trung tâm
        public ConversationDto? SelectedConversation { get; set; }
        public AccountDto CurrentAccount { get; set; } = new();

        // Panel phải (ví dụ thẻ sản phẩm/thông tin cửa hàng)
        public ProductCardVm? ProductCard { get; set; }
        public List<TipItemVm> SafetyTips { get; set; } = new();
    }

    public class ConversationListItemVm
    {
        public Guid ConversationId { get; set; }
        public string Title { get; set; } = "";          // Tên shop/người dùng
        public string? AvatarUrl { get; set; }
        public string? LastMessageSnippet { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }

        // + NEW: đánh dấu hội thoại được ghim
        public bool IsPinned { get; set; }
    }

    public class ProductCardVm
    {
        public string Title { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Badge { get; set; }                // Ví dụ: "Đã hỏi"
        public int? ItemsCount { get; set; }              // "832 sản phẩm"
        public string PriceRangeText { get; set; } = "";  // "27.000đ - 45.360đ"
        public string? BuyNowUrl { get; set; }
    }

    public class TipItemVm
    {
        public string Text { get; set; } = "";
        public string? LinkText { get; set; }
        public string? LinkUrl { get; set; }
    }
}
