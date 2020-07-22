using FluentSocket.Impl;
using System;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Push.DotNetty.ServerHost
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
