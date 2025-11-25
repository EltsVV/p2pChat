using MediatR;

namespace Chat.Core.Commands
{
    public interface ICommand : IRequest { }
    public interface IQuery<TResponse> : IRequest<TResponse> { }
    public interface IEvent : INotification { }
}