using Chat.Core.Models;
using Chat.Core.Enums;

namespace Chat.Core.Interfaces
{
    public interface IUserService
    {
        User CurrentUser { get; }
        void SetCurrentUser(string username, UserRole role = UserRole.User);
        void AddOrUpdateUser(User user);
        void RemoveUser(string username);
        IReadOnlyList<User> GetUsers();
        User? GetUser(string username);
        void UpdateUserRole(string username, UserRole role);
        void BanUser(string username);
        void UnbanUser(string username);
        bool IsUserBanned(string username);
    }
}