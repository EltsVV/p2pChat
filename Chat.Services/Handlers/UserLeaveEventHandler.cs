using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers;

public class UserLeaveEventHandler : INotificationHandler<UserLeaveEvent>
{
    private readonly IUIService _uiService;

    public UserLeaveEventHandler(IUIService uiService)
    {
        _uiService = uiService;
    }

    public Task Handle(UserLeaveEvent notification, CancellationToken cancellationToken)
    {
        _uiService.DisplaySystemMessage($"User {notification.user.Username} left the chat");
        return Task.CompletedTask;
    }
}