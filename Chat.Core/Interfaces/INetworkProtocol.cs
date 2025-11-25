using System.Net;

namespace Chat.Core.Interfaces
{
    public interface INetworkProtocol
    {
        Task BroadcastAsync(byte[] data);
        Task SendToAsync(IPEndPoint endpoint, byte[] data);
        event Action<IPEndPoint, byte[]>? MessageReceived;
        void Start();
        void Stop();
    }
}