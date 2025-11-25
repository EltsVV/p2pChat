using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Core.Enums;

namespace Chat.Services.Handlers
{
    public class BanUserHandler : IRequestHandler<BanUserCommand>
    {
        private readonly IUserService _userService;
        private readonly INetworkService _networkService;
        private readonly IMediator _mediator;

        public BanUserHandler(IUserService userService, INetworkService networkService, IMediator mediator)
        {
            _userService = userService;
            _networkService = networkService;
            _mediator = mediator;
        }

        public async Task Handle(BanUserCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _userService.CurrentUser;

            if (currentUser.Role != UserRole.Admin)
                throw new UnauthorizedAccessException("You must be an administrator to ban users");

            if (request.userName == currentUser.Username)
                throw new InvalidOperationException("You cannot ban yourself");

            var targetUser = _userService.GetUser(request.userName);
            if (targetUser == null)
                throw new InvalidOperationException($"User {request.userName} not found");

            if (targetUser.Role == UserRole.Admin)
                throw new InvalidOperationException("You cannot ban another administrator");

            _userService.BanUser(request.userName);

            var message = new ChatMessage
            {
                Sender = "System",
                Content = $"User {request.userName} has been banned",
                Type = MessageType.AdminCommand
            };

            await _networkService.SendBroadcastMessageAsync(message);
            await _mediator.Publish(new UserBannedEvent(request.userName, currentUser.Username), cancellationToken);
        }
    }

}