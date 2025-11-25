using System.Net.Sockets;
using System.Net;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Network.Protocols
{
    internal interface IGetActualPort
    {
        int GetActualPort();
    }
    internal class TcpP2P : INetworkProtocol, IGetActualPort
    {
        private TcpListener _tcpListener;
        private readonly int _port;
        private bool _isRunning;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isPortAutoSelected = false;
        private readonly ILogger<TcpP2P> _logger;

        public event Action<IPEndPoint, byte[]>? MessageReceived;

        public TcpP2P(int port, ILogger<TcpP2P> logger)
        {
            _port = port;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _logger.LogDebug("TCP listener created on port {Port}", port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create TCP listener on port {Port}", port);
                throw;
            }
        }

        public void Start()
        {
            try
            {
                _isRunning = true;
                _tcpListener.Start();
                _logger.LogInformation("TCP P2P started on port {Port}", ((IPEndPoint)_tcpListener.LocalEndpoint).Port);
                Task.Run(AcceptConnections);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                _logger.LogWarning("TCP port {Port} is already in use. Searching for available port...", _port);
                StartWithAutoPort();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP startup error on port {Port}", _port);
            }
        }

        private void StartWithAutoPort()
        {
            try
            {
                int availablePort = FindAvailablePort(_port + 1);
                _tcpListener = new TcpListener(IPAddress.Any, availablePort);
                _isRunning = true;
                _tcpListener.Start();
                _isPortAutoSelected = true;
                _logger.LogInformation("TCP P2P started on automatically selected port {Port}", availablePort);
                Task.Run(AcceptConnections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start TCP with auto port selection");
            }
        }

        public static int FindAvailablePort(int startPort = 12346, int maxAttempts = 50)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                int currentPort = startPort + i;

                if (currentPort > 65535)
                {
                    throw new InvalidOperationException("Reached maximum port number");
                }

                try
                {
                    using var tester = new TcpListener(IPAddress.Loopback, currentPort);
                    tester.Start();
                    tester.Stop();
                    return currentPort;
                }
                catch (SocketException)
                {
                    continue;
                }
            }

            throw new InvalidOperationException($"Could not find available TCP port after {maxAttempts} attempts");
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();

            try
            {
                _tcpListener?.Stop();
                _logger.LogInformation("TCP P2P stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TCP listener");
            }
        }

        public int GetActualPort()
        {
            try
            {
                return _tcpListener != null && _tcpListener.Server.IsBound ? ((IPEndPoint)_tcpListener.LocalEndpoint).Port : _port;
            }
            catch
            {
                return _port;
            }
        }

        public bool IsPortAutoSelected => _isPortAutoSelected;

        public Task BroadcastAsync(byte[] data) => Task.CompletedTask;

        public async Task SendToAsync(IPEndPoint endpoint, byte[] data)
        {
            try
            {
                _logger.LogDebug("Sending TCP message to {Endpoint}", endpoint);
                using var client = new TcpClient();
                await client.ConnectAsync(endpoint.Address, endpoint.Port);
                using var stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token);
                _logger.LogDebug("TCP message sent successfully to {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP send error to {Endpoint}", endpoint);
            }
        }

        private async Task AcceptConnections()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                    _logger.LogDebug("TCP connection accepted from {RemoteEndpoint}", client.Client.RemoteEndPoint);
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        _logger.LogError(ex, "TCP accept error");
                    }
                    break;
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);

                    if (bytesRead > 0)
                    {
                        var data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);

                        var remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
                        _logger.LogDebug("TCP message received from {RemoteEndpoint}, {BytesRead} bytes", remoteEndPoint, bytesRead);
                        MessageReceived?.Invoke(remoteEndPoint, data);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP client handling error");
            }
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
            _tcpListener?.Stop();
        }
    }
}