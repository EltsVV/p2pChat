using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Services.Handlers;

public class UserJoinedEventHandler : INotificationHandler<UserJoinenEvent>
{
    private readonly IUIService _uiService;
    private readonly IUserService _userService;
    private readonly ILogger<UserJoinedEventHandler> _logger;

    public UserJoinedEventHandler(IUIService uiService, IUserService userService, ILogger<UserJoinedEventHandler> logger)
    {
        _uiService = uiService;
        _userService = userService;
        _logger = logger;
    }

    public Task Handle(UserJoinenEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _userService.AddOrUpdateUser(notification.user);
            _logger.LogInformation("User {Username} added to UserService with endpoint {Endpoint}", notification.user.Username, notification.user.EndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user {Username} to UserService", notification.user.Username);
        }

        return Task.CompletedTask;
    }
}