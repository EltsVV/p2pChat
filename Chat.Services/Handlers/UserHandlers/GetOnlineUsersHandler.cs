using MediatR;
using Chat.Core.Commands;
using Chat.Core.Models;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers
{
    public class GetOnlineUsersHandler : IRequestHandler<GetUserOnlineQuery, IReadOnlyList<User>>
    {
        private readonly IUserService _userService;

        public GetOnlineUsersHandler(IUserService userService)
        {
            _userService = userService;
        }

        public Task<IReadOnlyList<User>> Handle(GetUserOnlineQuery request, CancellationToken cancellationToken)
        {
            var users = _userService.GetUsers();
            return Task.FromResult(users);
        }
    }

}