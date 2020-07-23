using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FluentSocket.DotNetty
{
    public class DotNettySocketSessionBuilder : ISocketSessionBuilder
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
