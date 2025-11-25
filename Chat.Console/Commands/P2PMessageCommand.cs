using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Console.Commands;

public class P2PMessageCommand : IChatCommand
{
    private readonly IMediator _mediator;
    private readonly IUIService _uiService;

    public string Command => "/p2p";
    public string Description => $"Send private message: {Command} [username] [message]";

    public P2PMessageCommand(IMediator mediator, IUIService uiService)
    {
        _mediator = mediator;
        _uiService = uiService;
    }

    public async Task ExecuteAsync(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                _uiService.DisplayErrorMessage($"Usage: {Command} [username] [message]");
                return;
            }

            var recipient = args[0];
            var message = string.Join(" ", args.Skip(1));

            var user = await _mediator.Send(new FindUserQuery(recipient));
            if (user == null)
            {
                _uiService.DisplayErrorMessage($"User '{recipient}' not found. Use /users to see online users.");
                return;
            }

            await _mediator.Send(new SendP2PMessageCommand(recipient, message));
            _uiService.DisplayMessage(recipient, message);
        }
        catch (Exception ex)
        {
            _uiService.DisplayErrorMessage($"Error sending private message: {ex.Message}");
        }
    }
}