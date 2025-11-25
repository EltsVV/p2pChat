using Chat.Core.Models;
using Chat.Network.Services;
using Chat.Core.Interfaces;
using Chat.Core.Enums;
using System.ComponentModel.Design;

namespace Chat.Services
{
    public class AdminService
    {
        private readonly IUserService _userService;
        private readonly INetworkService _networkService;

        public AdminService(IUserService userService, INetworkService networkService)
        {
            _userService = userService;
            _networkService = networkService;
        }

        public async Task BanUserAsync(string username)
        {
            Check(username);

            _userService.BanUser(username);

            var message = new ChatMessage
            {
                Sender = GetCurrentUsername(),
                Content = $"User {username} has been banned",
                Type = MessageType.AdminCommand
            };

            await _networkService.SendBroadcastMessageAsync(message);
        }


        public async Task UnbanUserAsync(string username)
        {
            Check(username);

            _userService.UnbanUser(username);

            var message = new ChatMessage
            {
                Sender = GetCurrentUsername(),
                Content = $"User {username} has been unbanned",
                Type = MessageType.AdminCommand
            };

            await _networkService.SendBroadcastMessageAsync(message);
        }

        private void Check(string username)
        {
            if (!IsCurrentUserAdmin())
                throw new UnauthorizedAccessException("You must be an administrator");

            if (username == GetCurrentUsername())
                throw new InvalidOperationException("You cannot ban yourself");

            var targetUser = _userService.GetUser(username);
            if (targetUser == null)
                throw new InvalidOperationException($"User {username} not found");

            if (targetUser.Role == UserRole.Admin)
                throw new InvalidOperationException("You cannot ban another administrator");
        }

        private bool IsCurrentUserAdmin()
        {
            var currentUser = _userService.GetUsers().FirstOrDefault(u => u.Username == GetCurrentUsername());
            return currentUser?.Role == UserRole.Admin;
        }

        private string GetCurrentUsername() => _userService.GetUsers().First().Username;
    }
}