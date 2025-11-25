using MediatR;
using Chat.Core.Commands;
using Chat.Core.Models;
using Chat.Core.Interfaces;
using Chat.Core.Enums;
using Microsoft.Extensions.Logging;

namespace Chat.Services.Handlers;

public class SendBroadcastMessageHandler : IRequestHandler<SendBroadcastMessageCommand>
{
    private readonly INetworkService _networkService;
    private readonly IUserService _userService;
    private readonly IEncryptionService _encryptionService;
    private readonly EmojiService _emojiService;
    private readonly ILogger<SendBroadcastMessageHandler> _logger;

    public SendBroadcastMessageHandler(INetworkService networkService, IUserService userService, IEncryptionService encryptionService, ILogger<SendBroadcastMessageHandler> logger)
    {
        _networkService = networkService;
        _userService = userService;
        _encryptionService = encryptionService;
        _emojiService = new EmojiService();
        _logger = logger;
    }

    public async Task Handle(SendBroadcastMessageCommand request, CancellationToken cancellationToken)
    {
        if (_userService.IsUserBanned(_userService.CurrentUser.Username))
            throw new InvalidOperationException("You are banned from the chat");

        var processedContent = _emojiService.ReplaceEmojis(request.content);

        var onlineUsers = _userService.GetUsers();
        var usersWithKeys = onlineUsers.Where(u => u.KeyExchanged && u.Username != _userService.CurrentUser.Username).ToList();

        string finalContent;
        bool isEncrypted = false;

        if (usersWithKeys.Any())
        {
            finalContent = _encryptionService.Encrypt(processedContent);
            isEncrypted = true;
            _logger.LogInformation("Sending encrypted message to {UserCount} users", usersWithKeys.Count);
        }
        else
        {
            finalContent = processedContent;
            _logger.LogWarning("No users with keys, sending plain text");
        }

        var message = new ChatMessage
        {
            Sender = _userService.CurrentUser.Username,
            Content = finalContent,
            Type = MessageType.Broadcast,
            IsEncrypted = isEncrypted
        };

        await _networkService.SendBroadcastMessageAsync(message);
    }
}