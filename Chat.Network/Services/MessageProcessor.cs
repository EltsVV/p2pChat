using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Chat.Core.Interfaces;
using Chat.Core.Models;
using Chat.Core.Enums;
using Chat.Core.Commands;
using Microsoft.Extensions.Logging;

namespace Chat.Network.Services;

public class MessageProcessor
{
    private readonly string _localUsername;
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IEncryptionService _encryptionService;
    private readonly INetworkService _networkService;
    private readonly ILogger<MessageProcessor> _logger;
    private readonly HashSet<string> _respondedUsers = new();

    public MessageProcessor(
        string username,
        IMediator mediator,
        IUserService userService,
        IEncryptionService encryptionService,
        INetworkService networkService,
        ILogger<MessageProcessor> logger)
    {
        _localUsername = username;
        _mediator = mediator;
        _userService = userService;
        _encryptionService = encryptionService;
        _networkService = networkService;
        _logger = logger;
    }

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task ProcessMessageAsync(IPEndPoint endpoint, byte[] data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            var message = JsonSerializer.Deserialize<ChatMessage>(json, _jsonOptions);

            if (message == null)
            {
                _logger.LogWarning("Received null message from {Endpoint}", endpoint);
                return;
            }

            if (message.Sender == _localUsername)
            {
                _logger.LogDebug("Ignoring message from self: {Sender}", message.Sender);
                return;
            }

            if (_userService.IsUserBanned(message.Sender))
            {
                _logger.LogInformation("Ignoring message from banned user: {Sender}", message.Sender);
                return;
            }

            _logger.LogDebug("Processing message from {Sender}, type: {MessageType}", message.Sender, message.Type);

            if (await ProcessAdminCommandAsync(message)) return;
            if (await ProcessUsernameConflictAsync(message, endpoint)) return;
            if (await ProcessKeyExchangeAsync(message)) return;

            await ProcessRegularMessageAsync(message, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message processing error from {Endpoint}", endpoint);
        }
    }

    private async Task<bool> ProcessAdminCommandAsync(ChatMessage message)
    {
        if (message.Type == MessageType.AdminCommand)
        {
            if (message.Content.Contains("has been banned"))
            {
                var bannedUser = ExtractUsernameFromBanMessage(message.Content);
                if (!string.IsNullOrEmpty(bannedUser))
                {
                    _logger.LogInformation("Processing ban sync for user: {BannedUser} from {Sender}", bannedUser, message.Sender);
                    _userService.BanUser(bannedUser);
                    _logger.LogInformation("User {BannedUser} banned via sync from {Sender}", bannedUser, message.Sender);
                }
                return true;
            }
            else if (message.Content.Contains("has been unbanned"))
            {
                var unbannedUser = ExtractUsernameFromUnbanMessage(message.Content);
                if (!string.IsNullOrEmpty(unbannedUser))
                {
                    _logger.LogInformation("Processing unban sync for user: {UnbannedUser} from {Sender}", unbannedUser, message.Sender);
                    _userService.UnbanUser(unbannedUser);
                }
                return true;
            }
        }
        return false;
    }

    private async Task<bool> ProcessUsernameConflictAsync(ChatMessage message, IPEndPoint endpoint)
    {
        var existingUser = _userService.GetUser(message.Sender);
        if (existingUser != null && existingUser.EndPoint != null &&
            !existingUser.EndPoint.Equals(new IPEndPoint(endpoint.Address, message.TcpPort)))
        {
            _logger.LogWarning("Username conflict detected for {Sender}", message.Sender);

            var conflictMessage = new ChatMessage
            {
                Sender = _localUsername,
                Content = $"Username {message.Sender} is already in use. Please choose a different name.",
                Type = MessageType.System,
                TcpPort = _networkService.GetTcpPort()
            };

            await _networkService.SendP2PMessageAsync(conflictMessage, endpoint);
            return true;
        }
        return false;
    }

    private async Task<bool> ProcessKeyExchangeAsync(ChatMessage message)
    {
        if (message.Type == MessageType.System)
        {
            if (message.Content.StartsWith("KEY_EXCHANGE_REQUEST:"))
            {
                var keyBase64 = message.Content.Substring("KEY_EXCHANGE_REQUEST:".Length);
                _logger.LogInformation("Processing key exchange request from {Sender}", message.Sender);
                await _mediator.Publish(new KeyExchangeRequestEvent(message.Sender, keyBase64));
                return true;
            }
            else if (message.Content.StartsWith("KEY_EXCHANGE_RESPONSE:"))
            {
                var keyBase64 = message.Content.Substring("KEY_EXCHANGE_RESPONSE:".Length);
                _logger.LogInformation("Processing key exchange response from {Sender}", message.Sender);
                await _mediator.Publish(new KeyExchangeResponseEvent(message.Sender, keyBase64));
                return true;
            }
        }
        return false;
    }

    private async Task ProcessRegularMessageAsync(ChatMessage message, IPEndPoint endpoint)
    {
        var userEndpoint = new IPEndPoint(endpoint.Address, message.TcpPort);
        var user = new User { Username = message.Sender, EndPoint = userEndpoint };

        await _mediator.Publish(new UserJoinenEvent(user));

        if (message.Type == MessageType.System && message.Content.Contains("joined"))
        {
            _logger.LogDebug("Processing user join: {Sender}", message.Sender);
            await ProcessUserJoinAsync(message.Sender, userEndpoint);
        }

        await _mediator.Publish(new MessageReceivedEvent(message));
    }

    private async Task ProcessUserJoinAsync(string username, IPEndPoint endpoint)
    {
        if (!_respondedUsers.Contains(username))
        {
            _respondedUsers.Add(username);
            await Task.Delay(new Random().Next(100, 500));
            await InitiateKeyExchangeAsync(username, endpoint);
        }
    }

    private async Task InitiateKeyExchangeAsync(string targetUser, IPEndPoint endpoint)
    {
        try
        {
            var ourKey = Convert.ToBase64String(_encryptionService.GetPublicKey());

            var keyMessage = new ChatMessage
            {
                Sender = _localUsername,
                Content = $"KEY_EXCHANGE_REQUEST:{ourKey}",
                Type = MessageType.System,
                TcpPort = _networkService.GetTcpPort()
            };

            _logger.LogDebug("Initiating key exchange with {TargetUser}", targetUser);
            await _networkService.SendP2PMessageAsync(keyMessage, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key exchange initiation error with {TargetUser}", targetUser);
        }
    }

    public async Task SendKeyExchangeResponseAsync(string targetUser, IPEndPoint endpoint)
    {
        try
        {
            var ourKey = Convert.ToBase64String(_encryptionService.GetPublicKey());

            var responseMessage = new ChatMessage
            {
                Sender = _localUsername,
                Content = $"KEY_EXCHANGE_RESPONSE:{ourKey}",
                Type = MessageType.System,
                TcpPort = _networkService.GetTcpPort()
            };

            _logger.LogDebug("Sending key exchange response to {TargetUser}", targetUser);
            await _networkService.SendP2PMessageAsync(responseMessage, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key exchange response error to {TargetUser}", targetUser);
        }
    }

    private string? ExtractUsernameFromBanMessage(string content)
    {
        if (content.StartsWith("User ") && content.Contains(" has been banned"))
        {
            var start = "User ".Length;
            var end = content.IndexOf(" has been banned");
            if (end > start)
            {
                return content.Substring(start, end - start);
            }
        }
        return null;
    }

    private string? ExtractUsernameFromUnbanMessage(string content)
    {
        if (content.StartsWith("User ") && content.Contains(" has been unbanned"))
        {
            var start = "User ".Length;
            var end = content.IndexOf(" has been unbanned");
            if (end > start)
            {
                return content.Substring(start, end - start);
            }
        }
        return null;
    }
}