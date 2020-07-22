using System;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Common.Services
{
    public class CustomSessionService : BaseSessionService
    {
        private readonly Action<ISocketSession> _sessionConnectedHandler;

        public CustomSessionService(Action<ISocketSession> sessionConnectedHandler)
        {
            _sessionConnectedHandler = sessionConnectedHandler;
        }


        public override ValueTask OnSessionConnectedAsync(ISocketSession session)
        {
            _sessionConnectedHandler(session);
            return new ValueTask();
        }
    }
}
