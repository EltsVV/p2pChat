using Chat.Core.Models;

namespace Chat.Core.Interfaces;

public interface IUIService
{
    void DisplayMessage(ChatMessage message);
    void DisplaySystemMessage(string message);
    void DisplayErrorMessage(string message);
    void DisplayUserList(IEnumerable<User> users);
    void DisplayP2PMessageConfirmation(string recipient, string message);
}