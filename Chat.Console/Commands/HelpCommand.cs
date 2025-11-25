using Chat.Core.Interfaces;

namespace Chat.Console.Commands;

public class HelpCommand : IChatCommand
{
    private readonly IEnumerable<IChatCommand> _commands;
    private readonly IUIService _uiService;

    public string Command => "/help";
    public string Description => "Show available commands";

    public HelpCommand(IEnumerable<IChatCommand> commands, IUIService uiService)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _uiService = uiService;
    }

    public Task ExecuteAsync(string[] args)
    {
        _uiService.DisplaySystemMessage("Available commands:");
        foreach (var command in _commands)
        {
            if (command != null)
            {
                System.Console.WriteLine($"  {command.Command} - {command.Description}");
            }
        }
        return Task.CompletedTask;
    }
}