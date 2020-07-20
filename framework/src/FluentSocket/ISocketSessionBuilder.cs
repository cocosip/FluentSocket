using System.Net;

namespace FluentSocket
{
    public interface ISocketSessionBuilder
    {
        ISocketSession BuildSession(string sessionId, EndPoint remoteEndPoint, EndPoint localEndPoint, SocketSessionState state);
    }
}
