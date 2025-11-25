using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Chat.Core.Models;
using Chat.Core.Enums;

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
        try
        {
            if (_userService.IsUserBanned(notification.chatMessage.Sender))
            {
                _logger.LogWarning("Blocked message from banned user {Sender}", notification.chatMessage.Sender);
                return Task.CompletedTask;
            }

            var message = notification.chatMessage;

            if (message.IsEncrypted)
            {
                _logger.LogDebug("Attempting to decrypt message from {Sender}", message.Sender);

                try
                {
                    var originalContent = message.Content;
                    message.Content = _encryptionService.Decrypt(message.Content);
                    message.IsEncrypted = false;

                    _logger.LogInformation("Successfully decrypted message from {Sender}", message.Sender);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Decryption error for message from {Sender}", message.Sender);

                    var errorMessage = new ChatMessage
                    {
                        Sender = "System",
                        Content = $"Unable to decrypt message from {message.Sender}. Key exchange may be incomplete.",
                        Type = MessageType.System,
                        Timestamp = DateTime.UtcNow
                    };

                    _uiService.DisplayMessage(errorMessage);

                    var encryptedMessage = new ChatMessage
                    {
                        Sender = message.Sender,
                        Content = "[Encrypted message - unable to decrypt]",
                        Type = MessageType.System,
                        Timestamp = message.Timestamp
                    };

                    _uiService.DisplayMessage(encryptedMessage);
                    return Task.CompletedTask;
                }
            }

            _uiService.DisplayMessage(message);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in MessageReceivedEventHandler");
            return Task.CompletedTask;
        }
    }
}