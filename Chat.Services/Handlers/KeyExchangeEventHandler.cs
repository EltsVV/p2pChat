using System.Net;
using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Chat.Core.Models;
using Chat.Core.Enums;

namespace Chat.Services.Handlers;

public class KeyExchangeEventHandler : INotificationHandler<KeyExchangeRequestEvent>, INotificationHandler<KeyExchangeResponseEvent>
{
    private readonly IUserService _userService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMediator _mediator;
    private readonly INetworkService _networkService;
    private readonly ILogger<KeyExchangeEventHandler> _logger;

    public KeyExchangeEventHandler(
        IUserService userService,
        IEncryptionService encryptionService,
        IMediator mediator,
        INetworkService networkService,
        ILogger<KeyExchangeEventHandler> logger)
    {
        _userService = userService;
        _encryptionService = encryptionService;
        _mediator = mediator;
        _networkService = networkService;
        _logger = logger;
    }

    public async Task Handle(KeyExchangeRequestEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received key exchange request from {Sender}", notification.Sender);

            var keyBytes = Convert.FromBase64String(notification.PublicKey);
            _encryptionService.SetPeerPublicKey(notification.Sender, keyBytes);

            var user = _userService.GetUser(notification.Sender);
            if (user != null)
            {
                user.PublicKey = notification.PublicKey;
                user.KeyExchanged = true;
                _logger.LogInformation("Key stored for user {Sender}", notification.Sender);
            }
            else _logger.LogWarning("User {Sender} not found when processing key request", notification.Sender);

            await SendKeyExchangeResponse(notification.Sender);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid public key format from {Sender}", notification.Sender);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during key exchange request from {Sender}", notification.Sender);
        }
    }

    public async Task Handle(KeyExchangeResponseEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing key exchange response from {Sender}", notification.Sender);

            var keyBytes = Convert.FromBase64String(notification.PublicKey);
            _encryptionService.SetPeerPublicKey(notification.Sender, keyBytes);

            var user = _userService.GetUser(notification.Sender);
            if (user != null)
            {
                user.PublicKey = notification.PublicKey;
                user.KeyExchanged = true;
                _logger.LogInformation("Key exchange completed successfully with {Sender}", notification.Sender);
            }
            else _logger.LogWarning("User {Sender} not found when processing key response", notification.Sender);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid public key format in response from {Sender}", notification.Sender);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during key exchange response from {Sender}", notification.Sender);
        }

        await Task.CompletedTask;
    }

    private async Task SendKeyExchangeResponse(string targetUser)
    {
        try
        {
            await Task.Delay(100);

            var user = _userService.GetUser(targetUser);
            if (user?.EndPoint == null)
            {
                await Task.Delay(200);
                user = _userService.GetUser(targetUser);

                if (user?.EndPoint == null)
                {
                    _logger.LogWarning("Cannot send key response to {TargetUser}: user not found after retry. Available users: {Users}", targetUser, string.Join(", ", _userService.GetUsers().Select(u => u.Username)));
                    return;
                }
            }

            var ourKey = Convert.ToBase64String(_encryptionService.GetPublicKey());

            var responseMessage = new ChatMessage
            {
                Sender = _userService.CurrentUser.Username,
                Content = $"KEY_EXCHANGE_RESPONSE:{ourKey}",
                Type = MessageType.System,
                TcpPort = _networkService.GetTcpPort()
            };

            _logger.LogInformation("Sending key exchange response to {TargetUser} at {Endpoint}", targetUser, user.EndPoint);
            await _networkService.SendP2PMessageAsync(responseMessage, user.EndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key exchange response error to {TargetUser}", targetUser);
        }
    }
}