using Chat.Core.Interfaces;
using Chat.Core.Models;

namespace Chat.Console.Services;

public class ConsoleUIService : IUIService
{
    public void DisplayMessage(ChatMessage message)
    {
        var originalColor = System.Console.ForegroundColor;

        try
        {
            switch (message.Type)
            {
                case Core.Enums.MessageType.System:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"[System] {message.Content}");
                    break;
                case Core.Enums.MessageType.Private:
                    System.Console.ForegroundColor = ConsoleColor.Magenta;
                    System.Console.WriteLine($"[Private from {message.Sender}]: {message.Content}");

                    break;
                case Core.Enums.MessageType.AdminCommand:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"[Admin] {message.Content}");
                    break;
                default:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    System.Console.WriteLine($"[{message.Timestamp:HH:mm}] {message.Sender}: {message.Content}");
                    break;
            }
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    public void DisplaySystemMessage(string message)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"[Info] {message}");
        System.Console.ForegroundColor = originalColor;
    }

    public void DisplayErrorMessage(string message)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"[Error] {message}");
        System.Console.ForegroundColor = originalColor;
    }

    public void DisplayUserList(IEnumerable<User> users)
    {
        System.Console.WriteLine("\n=== Online Users ===");
        foreach (var user in users)
        {
            var role = user.Role == Core.Enums.UserRole.Admin ? "[Admin]" : "";
            var status = user.IsBanned ? "[BANNED]" : "";
            System.Console.WriteLine($"  {user.Username} {role} {status}");
        }
        System.Console.WriteLine("====================\n");
    }
    public void DisplayMessage(string recipient, string message)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = ConsoleColor.Magenta;
        System.Console.WriteLine($"[Private to {recipient}]: {message}");
        System.Console.ForegroundColor = originalColor;
    }
}