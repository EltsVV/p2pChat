namespace Chat.Console.Commands;

public interface IChatCommand
{
    string Command { get; }
    string Description { get; }
    Task ExecuteAsync(string[] args);
}