using System.Threading.Tasks;

namespace FluentSocket.Impl
{
    public class BaseSessionService : ISessionService
    {
        public virtual ValueTask OnSessionConnectedAsync(ISocketSession session)
        {
            return new ValueTask();
        }

        public virtual ValueTask OnSessionClosedAsync(ISocketSession session)
        {
            return new ValueTask();
        }
    }
}
