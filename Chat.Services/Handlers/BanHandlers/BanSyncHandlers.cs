using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Services.Handlers;

public class UserBannedEventHandler : INotificationHandler<UserBannedEvent>
{
    private readonly IUserService _userService;
    private readonly ILogger<UserBannedEventHandler> _logger;

    public UserBannedEventHandler(IUserService userService, ILogger<UserBannedEventHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public Task Handle(UserBannedEvent notification, CancellationToken cancellationToken)
    {
        _userService.BanUser(notification.UserName);
        _logger.LogInformation("[Sync] User {UserName} was banned by {BannedBy}", notification.UserName, notification.BannedBy);
        return Task.CompletedTask;
    }
}

public class UserUnbannedEventHandler : INotificationHandler<UserUnbannedEvent>
{
    private readonly IUserService _userService;
    private readonly ILogger<UserUnbannedEventHandler> _logger;

    public UserUnbannedEventHandler(IUserService userService, ILogger<UserUnbannedEventHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public Task Handle(UserUnbannedEvent notification, CancellationToken cancellationToken)
    {
        _userService.UnbanUser(notification.UserName);
        _logger.LogInformation("[Sync] User {UserName} was unbanned by {UnbannedBy}", notification.UserName, notification.UnbannedBy);
        return Task.CompletedTask;
    }
}