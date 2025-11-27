

using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Chat.Services;

namespace Chat.Console.Commands
{
    internal class EmojiCommand : IChatCommand
    {
        IEmoji _emoji;
        IUIService _uiService;
        public string Command => "/emoji";

        public string Description => "Show emoji for use";

        public EmojiCommand(IUIService uIService, IEmoji emoji)
        {
            _emoji = emoji;
            _uiService = uIService;
        }

        public Task ExecuteAsync(string[] args)
        {
            try
            {
                var emojis = _emoji.GetAllEmjoi();
                _uiService.DisplayEmojiList(emojis);
            }
            catch (Exception ex)
            {
                _uiService.DisplayErrorMessage($"Error printing emoji: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
