using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Console.Commands;

public class UsersCommand : IChatCommand
{
    private readonly IMediator _mediator;
    private readonly IUIService _uiService;
    private readonly INetworkService _networkService;

    public string Command => "/users";
    public string Description => "Show online users";

    public UsersCommand(IMediator mediator, IUIService uiService, INetworkService networkService)
    {
        _mediator = mediator;
        _uiService = uiService;
        _networkService = networkService;
    }

    public async Task ExecuteAsync(string[] args)
    {
        try
        {
            await Task.Delay(800);

            var users = await _mediator.Send(new GetUserOnlineQuery());
            _uiService.DisplayUserList(users);
        }
        catch (Exception ex)
        {
            _uiService.DisplayErrorMessage($"Error getting users: {ex.Message}");
        }
    }
}