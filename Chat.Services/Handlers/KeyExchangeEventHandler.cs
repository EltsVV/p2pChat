using MediatR;
using Chat.Core.Commands;
using Chat.Core.Interfaces;

namespace Chat.Services.Handlers;

public class KeyExchangeEventHandler : INotificationHandler<KeyExchangeRequestEvent>, INotificationHandler<KeyExchangeResponseEvent>
{
    private readonly IUserService _userService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMediator _mediator;
    private readonly INetworkService _networkService;

    public KeyExchangeEventHandler(IUserService userService, IEncryptionService encryptionService,
        IMediator mediator, INetworkService networkService)
    {
        _userService = userService;
        _encryptionService = encryptionService;
        _mediator = mediator;
        _networkService = networkService;
    }

    public async Task Handle(KeyExchangeRequestEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var keyBytes = Convert.FromBase64String(notification.PublicKey);
            _encryptionService.SetPeerPublicKey(notification.Sender, keyBytes);

            var user = _userService.GetUser(notification.Sender);
            if (user != null)
            {
                user.PublicKey = notification.PublicKey;
                user.KeyExchanged = true;
                Console.WriteLine($"Key received from {notification.Sender}");
            }

            var userForResponse = _userService.GetUser(notification.Sender);
            if (userForResponse?.EndPoint != null)
            {
                await _networkService.SendKeyExchangeResponse(notification.Sender, userForResponse.EndPoint);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Key exchange error: {ex.Message}");
        }
    }

    public async Task Handle(KeyExchangeResponseEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var keyBytes = Convert.FromBase64String(notification.PublicKey);
            _encryptionService.SetPeerPublicKey(notification.Sender, keyBytes);

            var user = _userService.GetUser(notification.Sender);
            if (user != null)
            {
                user.PublicKey = notification.PublicKey;
                user.KeyExchanged = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Key exchange response error: {ex.Message}");
        }
    }
}
