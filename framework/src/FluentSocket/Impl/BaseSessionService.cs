using System.Threading.Tasks;

namespace FluentSocket
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
