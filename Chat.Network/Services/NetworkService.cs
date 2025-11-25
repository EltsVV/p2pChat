// Chat.Network/Services/NetworkService.cs
using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Core.Enums;
using Chat.Network.Protocols;

namespace Chat.Network.Services;

public class NetworkService : INetworkService
{
    private readonly INetworkProtocol _broadcast;
    private readonly INetworkProtocol _p2p;
    private readonly string _localUsername;
    private readonly int _tcpPort;
    private readonly MessageProcessor _messageProcessor;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public NetworkService(string username, int udpPort, int tcpPort, IMediator mediator, IUserService userService, IEncryptionService encryptionService)
    {
        _localUsername = username;
        _tcpPort = tcpPort;

        _broadcast = new MulticastProtocol(udpPort);
        _p2p = new TcpP2P(tcpPort);

        _messageProcessor = new MessageProcessor(username, mediator, userService, encryptionService, this);

        _broadcast.MessageReceived += OnBroadcastMessageReceived;
        _p2p.MessageReceived += OnDirectMessageReceived;
    }

    public void Start()
    {
        _broadcast.Start();
        _p2p.Start();
        _ = BroadcastUserJoin();
    }

    public void Stop()
    {
        _broadcast.Stop();
        _p2p.Stop();
    }

    public async Task SendBroadcastMessageAsync(ChatMessage message)
    {
        message.TcpPort = _tcpPort;
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var data = Encoding.UTF8.GetBytes(json);
        await _broadcast.BroadcastAsync(data);
    }

    public async Task SendP2PMessageAsync(ChatMessage message, IPEndPoint recipient)
    {
        message.TcpPort = _tcpPort;
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var data = Encoding.UTF8.GetBytes(json);
        await _p2p.SendToAsync(recipient, data);
    }

    public async Task SendKeyExchangeResponse(string targetUser, IPEndPoint endpoint)
        => await _messageProcessor.SendKeyExchangeResponseAsync(targetUser, endpoint);

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Broadcast user join error: {ex.Message}");
        }
    }

    public async Task BroadcastPresence() => await BroadcastUserJoin();

    private void OnBroadcastMessageReceived(IPEndPoint endpoint, byte[] data) => _ = _messageProcessor.ProcessMessageAsync(endpoint, data);

    private void OnDirectMessageReceived(IPEndPoint endpoint, byte[] data) => _ = _messageProcessor.ProcessMessageAsync(endpoint, data);
}