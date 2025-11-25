using MediatR;
using Chat.Core.Commands;
using Chat.Core.Models;
using Chat.Core.Interfaces;
using Chat.Core.Enums;
using Chat.Network.Services;


namespace Chat.Services.Handlers
{
    public class SendP2PMessageHandler : IRequestHandler<SendP2PMessageCommand>
    {
        private readonly INetworkService _networkService;
        private readonly IUserService _userService;
        private readonly IEncryptionService _encryptionService;
        private readonly EmojiService _emojiService;
        private readonly IMediator _mediator;

        public SendP2PMessageHandler(INetworkService networkService, IUserService userService, IEncryptionService encryptionService, IMediator mediator)
        {
            _networkService = networkService;
            _userService = userService;
            _encryptionService = encryptionService;
            _emojiService = new EmojiService();
            _mediator = mediator;
        }

        public async Task Handle(SendP2PMessageCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _userService.CurrentUser;

            if (_userService.IsUserBanned(currentUser.Username))
            {
                throw new InvalidOperationException("You are banned from the chat");
            }

            var user = _userService.GetUser(request.recipient);
            if (user == null || user.EndPoint == null)
            {
                throw new InvalidOperationException($"User {request.recipient} not found or offline");
            }

            if (_userService.IsUserBanned(request.recipient))
            {
                throw new InvalidOperationException($"User {request.recipient} is banned");
            }

            var processedContent = _emojiService.ReplaceEmojis(request.content);
            var encryptedContent = _encryptionService.Encrypt(processedContent);

            var message = new ChatMessage
            {
                Sender = currentUser.Username,
                Content = encryptedContent,
                Type = MessageType.Private,
                Recipient = request.recipient,
                IsEncrypted = true
            };

            await _networkService.SendP2PMessageAsync(message, user.EndPoint);
            await _mediator.Publish(new P2PMessageSendEvent(currentUser.Username, request.recipient, request.content), cancellationToken);
        }
    }
}