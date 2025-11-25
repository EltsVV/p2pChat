using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers;

public class MessageReceivedEventHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IUIService _uiService;
    private readonly IUserService _userService;
    private readonly IEncryptionService _encryptionService;

    public MessageReceivedEventHandler(IUIService uiService, IUserService userService, IEncryptionService encryptionService)
    {
        _uiService = uiService;
        _userService = userService;
        _encryptionService = encryptionService;
    }

    public Task Handle(MessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (_userService.IsUserBanned(notification.chatMessage.Sender))
        {
            Console.WriteLine($"BLOCKED: Received message from banned user {notification.chatMessage.Sender}");
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
                Console.WriteLine($"Decryption error: {ex.Message}");
            }
        }

        _uiService.DisplayMessage(message);
        return Task.CompletedTask;
    }
}