using System.Collections.Concurrent;
using Chat.Core.Models;
using Chat.Core.Enums;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Services;

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<string, User> _users = new();
    private User _currentUser = null!;
    private readonly ILogger<UserService> _logger;

    public event Action<User>? UserJoined;
    public event Action<User>? UserLeave;

    public User CurrentUser => _currentUser;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public void SetCurrentUser(string username, UserRole role = UserRole.User)
    {
        _currentUser = new User { Username = username, Role = role };
        AddOrUpdateUser(_currentUser);
        _logger.LogInformation("Current user set to {Username} with role {Role}", username, role);
    }

    public void AddOrUpdateUser(User user)
    {
        if (_users.TryGetValue(user.Username, out var existingUser))
        {
            if (user.EndPoint != null)
                existingUser.EndPoint = user.EndPoint;
            existingUser.LastSeen = DateTime.UtcNow;
            _logger.LogDebug("Updated user {Username}", user.Username);
        }
        else
        {
            _users[user.Username] = user;
            _logger.LogInformation("New user added: {Username}", user.Username);
            if (user.Username != _currentUser.Username)
                UserJoined?.Invoke(user);
        }
    }

    public void RemoveUser(string username)
    {
        if (_users.TryRemove(username, out var user))
        {
            _logger.LogInformation("User removed: {Username}", username);
            UserLeave?.Invoke(user);
        }
    }

    public IReadOnlyList<User> GetUsers() => _users.Values.ToList().AsReadOnly();

    public User? GetUser(string username) => _users.TryGetValue(username, out var user) ? user : null;

    public void UpdateUserRole(string username, UserRole role)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.Role = role;
            _logger.LogInformation("User {Username} role updated to {Role}", username, role);
        }
    }

    public void BanUser(string username)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.IsBanned = true;
            _logger.LogInformation("User {Username} banned (still in online list)", username);
        }
    }

    public void UnbanUser(string username)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.IsBanned = false;
            _logger.LogInformation("User {Username} unbanned", username);
        }
    }

    public bool IsUserBanned(string username) => _users.TryGetValue(username, out var user) && user.IsBanned;
}