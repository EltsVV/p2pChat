using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Core.Enums;
using Chat.Network.Protocols;
using Microsoft.Extensions.Logging;

namespace Chat.Network.Services;

public class NetworkService : INetworkService
{
    private readonly INetworkProtocol _broadcast;
    private readonly INetworkProtocol _p2p;
    private readonly string _localUsername;
    private int _tcpPort;
    private readonly MessageProcessor _messageProcessor;
    private readonly ILogger<NetworkService> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public NetworkService(string username, int udpPort, int tcpPort, IMediator mediator, IUserService userService, IEncryptionService encryptionService, ILogger<NetworkService> logger, ILoggerFactory loggerFactory)
    {
        _localUsername = username;
        _tcpPort = tcpPort;
        _logger = logger;
        _encryptionService = encryptionService;

        _broadcast = new MulticastProtocol(udpPort, loggerFactory.CreateLogger<MulticastProtocol>());
        _p2p = new TcpP2P(tcpPort, loggerFactory.CreateLogger<TcpP2P>());

        _messageProcessor = new MessageProcessor(
            username, mediator, userService, encryptionService, this,
            loggerFactory.CreateLogger<MessageProcessor>());

        _broadcast.MessageReceived += OnBroadcastMessageReceived;
        _p2p.MessageReceived += OnDirectMessageReceived;

        _logger.LogInformation("Network service initialized for user {Username}", username);
    }

    public void Start()
    {
        _logger.LogInformation("Starting network services...");
        _broadcast.Start();
        _p2p.Start();

        if (_p2p is IGetActualPort) _tcpPort = ((IGetActualPort)_p2p).GetActualPort();

        _ = BroadcastUserJoin();
        _logger.LogInformation("Network services started on TCP port {TcpPort}", _tcpPort);
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping network services...");
        _broadcast.Stop();
        _p2p.Stop();
        _logger.LogInformation("Network services stopped");
    }

    public async Task SendBroadcastMessageAsync(ChatMessage message)
    {
        try
        {
            message.TcpPort = _tcpPort;
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var data = Encoding.UTF8.GetBytes(json);
            await _broadcast.BroadcastAsync(data);
            _logger.LogDebug("Broadcast message sent: {MessageType}", message.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast message");
        }
    }

    public async Task SendP2PMessageAsync(ChatMessage message, IPEndPoint recipient)
    {
        try
        {
            message.TcpPort = _tcpPort;
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var data = Encoding.UTF8.GetBytes(json);
            await _p2p.SendToAsync(recipient, data);
            _logger.LogDebug("P2P message sent to {Recipient}", recipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending P2P message to {Recipient}", recipient);
        }
    }

    public async Task SendKeyExchangeResponse(string targetUser, IPEndPoint endpoint)
    {
        _logger.LogDebug("Sending key exchange response to {TargetUser}", targetUser);
        await _messageProcessor.SendKeyExchangeResponseAsync(targetUser, endpoint);
    }

    public int GetTcpPort() => _tcpPort;

    public async Task BroadcastUserJoin()
    {
        try
        {
            var joinMessage = new ChatMessage
            {
                Sender = _localUsername,
                Content = $"{_localUsername} joined the chat",
                Type = MessageType.System,
                TcpPort = _tcpPort
            };

            await SendBroadcastMessageAsync(joinMessage);
            _logger.LogInformation("User join broadcast sent");

            await Task.Delay(500);
            await BroadcastOurKey();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting user join");
        }
    }

    private async Task BroadcastOurKey()
    {
        try
        {
            var ourKey = Convert.ToBase64String(_encryptionService.GetPublicKey());

            var keyMessage = new ChatMessage
            {
                Sender = _localUsername,
                Content = $"KEY_EXCHANGE_REQUEST:{ourKey}",
                Type = MessageType.System,
                TcpPort = _tcpPort
            };

            await SendBroadcastMessageAsync(keyMessage);
            _logger.LogInformation("Our public key broadcasted to all users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting our key");
        }
    }

    public async Task BroadcastPresence()
    {
        _logger.LogDebug("Broadcasting presence");
        await BroadcastUserJoin();
    }

    private void OnBroadcastMessageReceived(IPEndPoint endpoint, byte[] data)
    {
        _logger.LogDebug("Broadcast message received from {Endpoint}", endpoint);
        _ = _messageProcessor.ProcessMessageAsync(endpoint, data);
    }

    private void OnDirectMessageReceived(IPEndPoint endpoint, byte[] data)
    {
        _logger.LogDebug("Direct message received from {Endpoint}", endpoint);
        _ = _messageProcessor.ProcessMessageAsync(endpoint, data);
    }
}