using System.Net;
using System.Net.Sockets;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<MulticastProtocol> _logger;

        public event Action<IPEndPoint, byte[]>? MessageReceived;

        public MulticastProtocol(int port, ILogger<MulticastProtocol> logger, string multicastAddress = "239.255.255.250")
        {
            _port = port;
            _multicastAddress = IPAddress.Parse(multicastAddress);
            _logger = logger;

            _sendClient = new UdpClient();
            _sendClient.EnableBroadcast = true;

            _receiveClient = new UdpClient();
            _receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _cancellationTokenSource = new CancellationTokenSource();

            _logger.LogDebug("Multicast protocol initialized on address {Address}:{Port}", multicastAddress, port);
        }

        public void Start()
        {
            _isRunning = true;

            try
            {
                _receiveClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
                _receiveClient.JoinMulticastGroup(_multicastAddress);

                _logger.LogInformation("Multicast started on {Address}:{Port}", _multicastAddress, _port);
                Task.Run(StartReceiving);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Multicast start error on {Address}:{Port}", _multicastAddress, _port);
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
            _logger.LogInformation("Multicast stopped");
        }

        public async Task BroadcastAsync(byte[] data)
        {
            try
            {
                var endpoint = new IPEndPoint(_multicastAddress, _port);
                await _sendClient.SendAsync(data, data.Length, endpoint);
                _logger.LogDebug("Multicast message sent, {Bytes} bytes", data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Multicast send error");
            }
        }

        public async Task SendToAsync(IPEndPoint endpoint, byte[] data)
        {
            try
            {
                await _sendClient.SendAsync(data, data.Length, endpoint);
                _logger.LogDebug("UDP message sent to {Endpoint}, {Bytes} bytes", endpoint, data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UDP send error to {Endpoint}", endpoint);
            }
        }

        private async Task StartReceiving()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _receiveClient.ReceiveAsync(_cancellationTokenSource.Token);
                    _logger.LogDebug("Multicast message received from {RemoteEndpoint}, {Bytes} bytes",
                        result.RemoteEndPoint, result.Buffer.Length);
                    MessageReceived?.Invoke(result.RemoteEndPoint, result.Buffer);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        _logger.LogError(ex, "Multicast receive error");
                        await Task.Delay(1000);
                    }
                }
            }
        }
    }
}