namespace VHS_frontend.Areas.Provider.Models.ChatDTOs
{
    public enum MessageStatus { Pending, Sent, Delivered, Seen }

    public class MessageAccountDto
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public bool? IsDeleted { get; set; }
    }

    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderAccountId { get; set; }

        public string? Body { get; set; }
        public string MessageType { get; set; } = "Text";
        public Guid? ReplyToMessageId { get; set; }
        public string? ImageUrl { get; set; }
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public MessageAccountDto Sender { get; set; } = null!;
        public MessageDto? ReplyTo { get; set; }

        // tiện cho UI
        public bool IsMine { get; set; }

        public MessageStatus Status { get; set; } = MessageStatus.Sent;
    }

    public class ConversationDto
    {
        public Guid ConversationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }

        // Hai người trong hội thoại (1–1)
        public MessageAccountDto ParticipantA { get; set; } = null!;
        public MessageAccountDto ParticipantB { get; set; } = null!;

        // Trạng thái cá nhân hoá theo user hiện tại
        public bool IsHiddenForMe { get; set; }
        public bool IsMutedForMe { get; set; }

        // Hiển thị ở sidebar
        public string Title { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public string? LastMessageSnippet { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public bool IsPinned { get; set; }

        public List<MessageDto> Messages { get; set; } = new();
    }

    public class ConversationListItemVm
    {
        public Guid ConversationId { get; set; }
        public string Title { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public string? LastMessageSnippet { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public bool IsPinned { get; set; }


    }

    public class ProductCardVm
    {
        public string Title { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Badge { get; set; }
        public int? ItemsCount { get; set; }
        public string PriceRangeText { get; set; } = "";
        public string? BuyNowUrl { get; set; }
    }

    public class TipItemVm
    {
        public string Text { get; set; } = "";
        public string? LinkText { get; set; }
        public string? LinkUrl { get; set; }
    }

    public class ChatPageVm
    {
        public List<ConversationListItemVm> Conversations { get; set; } = new();
        public Guid? SelectedConversationId { get; set; }
        public ConversationDto? SelectedConversation { get; set; }
        public MessageAccountDto CurrentAccount { get; set; } = new();

        public ProductCardVm? ProductCard { get; set; }
        public List<TipItemVm> SafetyTips { get; set; } = new();
    }

    
}
