using System.Threading.Tasks;

namespace FluentSocket
{
    public interface ISessionService : ISocketService
    {
        ValueTask OnSessionConnectedAsync(ISocketSession session);

        ValueTask OnSessionClosedAsync(ISocketSession session);
    }
}
