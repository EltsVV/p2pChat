using System.Runtime.InteropServices;

namespace Chat.Services
{
    public interface IEmoji {
        string ReplaceEmojis(string text);
        Dictionary<string, string> GetAllEmjoi();
    }
    
    public class EmojiService : IEmoji
    {
        private readonly Dictionary<string, string> _emojis;
        public EmojiService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _emojis = new()
                {
                    { ":)", "\u263A" },
                    { "<3", "\u2665" },
                    { ":diamond:", "\u2666" },
                    { ":club:", "\u2663" },
                    { ":spade:", "\u2660" },
                };
            else
                _emojis = new()
                {
                    { ":)", "😊" },
                    { ":(", "😞" },
                    { ":D", "😃" },
                    { ":P", "😛" },
                    { ";)", "😉" },
                };
        }

        public Dictionary<string, string> GetAllEmjoi() => _emojis;

        public string ReplaceEmojis(string text)
        {
            foreach (var emoji in _emojis) text = text.Replace(emoji.Key, emoji.Value);
            return text;
        }
    }
}