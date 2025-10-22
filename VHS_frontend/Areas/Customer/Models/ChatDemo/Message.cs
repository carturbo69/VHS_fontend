namespace VHS_frontend.Areas.Customer.Models.ChatDemo
{
    public class Message
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderAccountId { get; set; }

        public string? Body { get; set; }
        public string MessageType { get; set; } = "Text"; // "Text" | "Image" | "TextWithImage" | "System"
        public Guid? ReplyToMessageId { get; set; }

        // Chỉ lưu đường dẫn ảnh
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? Metadata { get; set; }

        public virtual Conversation Conversation { get; set; } = null!;
        public virtual Account Sender { get; set; } = null!;
        public virtual Message? ReplyTo { get; set; }
        public virtual ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();
    }

    public class MessageReceipt
    {
        public Guid MessageId { get; set; }
        public Guid RecipientAccountId { get; set; }
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public virtual Message Message { get; set; } = null!;
        public virtual Account Recipient { get; set; } = null!;
    }

    public class Conversation
    {
        public Guid ConversationId { get; set; }
        public string Type { get; set; } = null!; // "UserProvider" | "UserAdmin"
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public Guid CreatedByAccountId { get; set; }

        public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class ConversationParticipant
    {
        public Guid ConversationId { get; set; }
        public Guid AccountId { get; set; }
        public string RoleInConversation { get; set; } = null!;
        public DateTime JoinedAt { get; set; }

        public virtual Conversation Conversation { get; set; } = null!;
        public virtual Account Account { get; set; } = null!;
    }
}
