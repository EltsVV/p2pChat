using Chat.Core.Enums;

namespace Chat.Core.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Sender { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Signature { get; set; }
        public string? Recipient { get; set; }
        public bool IsEncrypted { get; set; }
        public int TcpPort { get; set; }
    }
}
