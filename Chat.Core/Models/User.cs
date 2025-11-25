using System.Net;
using Chat.Core.Enums;

namespace Chat.Core.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public IPEndPoint? EndPoint { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public bool IsBanned { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public string? PublicKey { get; set; }
        public bool KeyExchanged { get; set; }
    }
}