using System;
using System.Net;

namespace FluentSocket
{
    public class DefaultSocketSessionBuilder : ISocketSessionBuilder
    {
        public ISocketSession BuildSession(string sessionId, EndPoint remoteEndPoint, EndPoint localEndPoint, SocketSessionState state)
        {
            var session = new SocketSession()
            {
                SessionId = sessionId,
                RemoteEndPoint = remoteEndPoint,
                LocalEndPoint = localEndPoint,
                StartTime = DateTime.Now,
                State = state
            };
            return session;
        }
    }
}
