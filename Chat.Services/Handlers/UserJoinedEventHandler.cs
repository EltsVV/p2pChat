using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers;

public class UserJoinedEventHandler : INotificationHandler<UserJoinenEvent>
{
    private readonly IUIService _uiService;
    private readonly IUserService _userService;

    public UserJoinedEventHandler(IUIService uiService, IUserService userService)
    {
        _uiService = uiService;
        _userService = userService;
    }

    public Task Handle(UserJoinenEvent notification, CancellationToken cancellationToken)
    {
        if (_userService.IsUserBanned(notification.user.Username))
        {
            _userService.AddOrUpdateUser(notification.user);
            return Task.CompletedTask;
        }

        _userService.AddOrUpdateUser(notification.user);
        return Task.CompletedTask;
    }
}