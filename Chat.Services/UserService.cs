using System.Collections.Concurrent;
using Chat.Core.Models;
using Chat.Core.Enums;
using Chat.Core.Interfaces;

namespace Chat.Services;

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<string, User> _users = new();
    private User _currentUser = null!;

    public event Action<User>? UserJoined;
    public event Action<User>? UserLeave;

    public User CurrentUser => _currentUser;

    public void SetCurrentUser(string username, UserRole role = UserRole.User)
    {
        _currentUser = new User { Username = username, Role = role };
        AddOrUpdateUser(_currentUser);
    }

    public void AddOrUpdateUser(User user)
    {
        if (_users.TryGetValue(user.Username, out var existingUser))
        {
            if (user.EndPoint != null)
                existingUser.EndPoint = user.EndPoint;
            existingUser.LastSeen = DateTime.UtcNow;
        }
        else
        {
            _users[user.Username] = user;
            if (user.Username != _currentUser.Username)
                UserJoined?.Invoke(user);
        }
    }

    public void RemoveUser(string username)
    {
        if (_users.TryRemove(username, out var user))
            UserLeave?.Invoke(user);
    }

    public IReadOnlyList<User> GetUsers() => _users.Values.ToList().AsReadOnly();

    public User? GetUser(string username) => _users.TryGetValue(username, out var user) ? user : null;

    public void UpdateUserRole(string username, UserRole role)
    {
        if (_users.TryGetValue(username, out var user))
            user.Role = role;
    }

    public void BanUser(string username)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.IsBanned = true;
            Console.WriteLine($"User {username} banned (still in online list)");
        }
    }

    public void UnbanUser(string username)
    {
        if (_users.TryGetValue(username, out var user))
        {
            user.IsBanned = false;
            Console.WriteLine($"User {username} unbanned");
        }
    }

    public bool IsUserBanned(string username) => _users.TryGetValue(username, out var user) && user.IsBanned;
}