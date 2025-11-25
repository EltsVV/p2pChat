using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers;

public class UserBannedEventHandler : INotificationHandler<UserBannedEvent>
{
    private readonly IUserService _userService;

    public UserBannedEventHandler(IUserService userService)
    {
        _userService = userService;
    }

    public Task Handle(UserBannedEvent notification, CancellationToken cancellationToken)
    {
        _userService.BanUser(notification.UserName);
        Console.WriteLine($"[Sync] User {notification.UserName} was banned by {notification.BannedBy}");
        return Task.CompletedTask;
    }
}

public class UserUnbannedEventHandler : INotificationHandler<UserUnbannedEvent>
{
    private readonly IUserService _userService;

    public UserUnbannedEventHandler(IUserService userService)
    {
        _userService = userService;
    }

    public Task Handle(UserUnbannedEvent notification, CancellationToken cancellationToken)
    {
        _userService.UnbanUser(notification.UserName);
        Console.WriteLine($"[Sync] User {notification.UserName} was unbanned by {notification.UnbannedBy}");
        return Task.CompletedTask;
    }
}

