namespace Chat.Services
{

    public class EmojiService
    {
        private readonly Dictionary<string, string> _emojis = new()
        {
            { ":)", "😊" },
            { ":(", "😞" },
            { ":D", "😃" },
            { ":P", "😛" },
            { ";)", "😉" },
        };

        public string ReplaceEmojis(string text)
        {
            foreach (var emoji in _emojis) text = text.Replace(emoji.Key, emoji.Value);
            return text;
        }
    }
}