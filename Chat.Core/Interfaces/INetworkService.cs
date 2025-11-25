using System.Net;
using Chat.Core.Models;

namespace Chat.Core.Interfaces
{
    public interface INetworkService
    {
        void Start();
        void Stop();
        Task SendBroadcastMessageAsync(ChatMessage message);
        Task SendP2PMessageAsync(ChatMessage message, IPEndPoint recipient);
        Task SendKeyExchangeResponse(string targetUser, IPEndPoint endpoint);
        Task BroadcastUserJoin();
        Task BroadcastPresence();
        int GetTcpPort();
    }
}