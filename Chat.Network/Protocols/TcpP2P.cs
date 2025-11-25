using System.Net.Sockets;
using System.Net;
using Chat.Core.Interfaces;

namespace Chat.Network.Protocols
{
    internal class TcpP2P : INetworkProtocol
    {
        private TcpListener _tcpListener;
        private readonly int _port;
        private bool _isRunning;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isPortAutoSelected = false;

        public event Action<IPEndPoint, byte[]>? MessageReceived;

        public TcpP2P(int port = 12346)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create TCP listener on port {port}: {ex.Message}");
                throw;
            }
        }

        public void Start()
        {
            try
            {
                _isRunning = true;
                _tcpListener.Start();
                Console.WriteLine($"TCP P2P started on port {((IPEndPoint)_tcpListener.LocalEndpoint).Port}");
                Task.Run(AcceptConnections);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine($"TCP port {_port} is already in use. Searching for available port...");
                StartWithAutoPort();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP startup error: {ex.Message}");
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
                Console.WriteLine($"TCP P2P started on automatically selected port {availablePort}");
                Task.Run(AcceptConnections);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start TCP with auto port selection: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping TCP listener: {ex.Message}");
            }
        }

        public int GetActualPort()
        {
            try
            {
                return _tcpListener != null && _tcpListener.Server.IsBound
                    ? ((IPEndPoint)_tcpListener.LocalEndpoint).Port
                    : _port;
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
                using var client = new TcpClient();
                await client.ConnectAsync(endpoint.Address, endpoint.Port);
                using var stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP send error: {ex.Message}");
            }
        }

        private async Task AcceptConnections()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
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
                        Console.WriteLine($"TCP accept error: {ex.Message}");
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
                        MessageReceived?.Invoke(remoteEndPoint, data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP client handling error: {ex.Message}");
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