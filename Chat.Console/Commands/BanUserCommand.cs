using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Console.Commands;

public class BanUserConsoleCommand : IChatCommand
{
    private readonly IMediator _mediator;
    private readonly IUIService _uiService;

    public string Command => "/ban";
    public string Description => $"Ban user: {Command} [username]";

    public BanUserConsoleCommand(IMediator mediator, IUIService uiService)
    {
        _mediator = mediator;
        _uiService = uiService;
    }

    public async Task ExecuteAsync(string[] args)
    {
        try
        {
            if (args.Length < 1)
            {
                _uiService.DisplayErrorMessage($"Usage: {Command} [username]");
                return;
            }

            var username = args[0];

            await _mediator.Send(new BanUserCommand(username));
            _uiService.DisplaySystemMessage($"User {username} has been banned");
        }
        catch (Exception ex)
        {
            _uiService.DisplayErrorMessage($"Error banning user: {ex.Message}");
        }
    }
}