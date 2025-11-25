using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Core.Enums;
using Microsoft.Extensions.Logging;

namespace Chat.Services.Handlers;

public class MessageReceivedEventHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IUIService _uiService;
    private readonly IUserService _userService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<MessageReceivedEventHandler> _logger;

    public MessageReceivedEventHandler(IUIService uiService, IUserService userService, IEncryptionService encryptionService, ILogger<MessageReceivedEventHandler> logger)
    {
        _uiService = uiService;
        _userService = userService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public Task Handle(MessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (_userService.IsUserBanned(notification.chatMessage.Sender))
        {
            _logger.LogWarning($"BLOCKED: Received message from banned user {notification.chatMessage.Sender}");
            return Task.CompletedTask;
        }

        var message = notification.chatMessage;

        if (message.IsEncrypted)
        {
            try
            {
                message.Content = _encryptionService.Decrypt(message.Content);
                message.IsEncrypted = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption error for message from {Sender}", message.Sender);

                var errorMessage = new ChatMessage
                {
                    Sender = "System",
                    Content = $"Failed to decrypt message from {message.Sender}",
                    Type = MessageType.System,
                    Timestamp = DateTime.UtcNow
                };

                _uiService.DisplayMessage(errorMessage);
                return Task.CompletedTask;
            }
        }

        _uiService.DisplayMessage(message);
        return Task.CompletedTask;
    }
}