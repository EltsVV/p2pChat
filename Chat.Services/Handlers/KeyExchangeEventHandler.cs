using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;
using Microsoft.Extensions.Logging;

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
            _logger.LogInformation("Processing key exchange request from {Sender}", notification.Sender);

            var keyBytes = Convert.FromBase64String(notification.PublicKey);
            _encryptionService.SetPeerPublicKey(notification.Sender, keyBytes);

            var user = _userService.GetUser(notification.Sender);
            if (user != null)
            {
                user.PublicKey = notification.PublicKey;
                user.KeyExchanged = true;
                _logger.LogInformation("Key successfully received and stored for user {Sender}", notification.Sender);
            }
            else
            {
                _logger.LogWarning("User {Sender} not found in user service during key exchange", notification.Sender);
            }

            var userForResponse = _userService.GetUser(notification.Sender);
            if (userForResponse?.EndPoint != null)
            {
                _logger.LogDebug("Sending key exchange response to {Sender}", notification.Sender);
                await _networkService.SendKeyExchangeResponse(notification.Sender, userForResponse.EndPoint);
                _logger.LogInformation("Key exchange response sent to {Sender}", notification.Sender);
            }
            else
            {
                _logger.LogWarning("Cannot send key exchange response to {Sender}: endpoint not available", notification.Sender);
            }
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
            else
            {
                _logger.LogWarning("User {Sender} not found when processing key exchange response", notification.Sender);
            }
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
}