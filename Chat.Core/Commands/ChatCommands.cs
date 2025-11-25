using Chat.Core.Models;

namespace Chat.Core.Commands
{
    public record SendBroadcastMessageCommand(string content) : ICommand;
    public record SendP2PMessageCommand(string recipient, string content) : ICommand;
    public record BanUserCommand(string userName) : ICommand;
    public record UnBanUserCommand(string userName) : ICommand;

    public record GetUserOnlineQuery : IQuery<IReadOnlyList<User>>;
    public record FindUserQuery(string userName) : IQuery<User?>;

    public record MessageReceivedEvent(ChatMessage chatMessage) : IEvent;
    public record P2PMessageSendEvent(string senger, string recipient, string message) : IEvent;
    public record UserJoinenEvent(User user) : IEvent;
    public record UserLeaveEvent(User user) : IEvent;

    public record UserBannedEvent(string UserName, string BannedBy) : IEvent;
    public record UserUnbannedEvent(string UserName, string UnbannedBy) : IEvent;

    public record KeyExchangeRequestEvent(string Sender, string PublicKey) : IEvent;
    public record KeyExchangeResponseEvent(string Sender, string PublicKey) : IEvent;
}