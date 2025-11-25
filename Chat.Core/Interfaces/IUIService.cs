using Chat.Core.Models;

namespace Chat.Core.Interfaces;

public interface IUIService
{
    void DisplayMessage(ChatMessage message);
    void DisplayMessage(string recipient, string message);
    void DisplaySystemMessage(string message);
    void DisplayErrorMessage(string message);
    void DisplayUserList(IEnumerable<User> users);
}