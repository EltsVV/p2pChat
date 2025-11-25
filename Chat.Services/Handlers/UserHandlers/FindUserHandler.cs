using MediatR;
using Chat.Core.Commands;
using Chat.Core.Models;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers
{
    public class FindUserHandler : IRequestHandler<FindUserQuery, User?>
    {
        private readonly IUserService _userService;

        public FindUserHandler(IUserService userService)
        {
            _userService = userService;
        }

        public Task<User?> Handle(FindUserQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_userService.GetUser(request.userName));
        }
    }
}