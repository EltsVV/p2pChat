using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Console.Commands;

public class UnbanUserConsoleCommand : IChatCommand
{
    private readonly IMediator _mediator;
    private readonly IUIService _uiService;

    public string Command => "/unban";
    public string Description => $"Unban user: {Command} [username]";

    public UnbanUserConsoleCommand(IMediator mediator, IUIService uiService)
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

            await _mediator.Send(new UnBanUserCommand(username));
            _uiService.DisplaySystemMessage($"User {username} has been unbanned");
        }
        catch (Exception ex)
        {
            _uiService.DisplayErrorMessage($"Error unbanning user: {ex.Message}");
        }
    }
}