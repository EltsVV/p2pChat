using System.Net;
using System.Net.Sockets;
using Chat.Core.Interfaces;

namespace Chat.Network.Protocols
{
    public class MulticastProtocol : INetworkProtocol
    {
        private readonly UdpClient _sendClient;
        private readonly UdpClient _receiveClient;
        private readonly IPAddress _multicastAddress;
        private readonly int _port;
        private bool _isRunning;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public event Action<IPEndPoint, byte[]>? MessageReceived;

        public MulticastProtocol(int port = 12345, string multicastAddress = "239.255.255.250")
        {
            _port = port;
            _multicastAddress = IPAddress.Parse(multicastAddress);

            _sendClient = new UdpClient();
            _sendClient.EnableBroadcast = true;

            _receiveClient = new UdpClient();
            _receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _isRunning = true;

            try
            {
                _receiveClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));

                _receiveClient.JoinMulticastGroup(_multicastAddress);

                Task.Run(StartReceiving);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Multicast start error: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _receiveClient?.DropMulticastGroup(_multicastAddress);
            _sendClient?.Close();
            _receiveClient?.Close();
        }

        public async Task BroadcastAsync(byte[] data)
        {
            try
            {
                var endpoint = new IPEndPoint(_multicastAddress, _port);
                await _sendClient.SendAsync(data, data.Length, endpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Multicast send error: {ex.Message}");
            }
        }

        public async Task SendToAsync(IPEndPoint endpoint, byte[] data)
        {
            await _sendClient.SendAsync(data, data.Length, endpoint);
        }

        private async Task StartReceiving()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _receiveClient.ReceiveAsync(_cancellationTokenSource.Token);
                    MessageReceived?.Invoke(result.RemoteEndPoint, result.Buffer);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    if (_isRunning)
                        await Task.Delay(1000);
                }
            }
        }
    }
}