using FluentSocket.Protocols;
using FluentSocket.Traffic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace FluentSocket
{
    public interface ISocketServer
    {
        /// <summary>Listening ip address and port
        /// </summary>
        IPEndPoint ListeningEndPoint { get; }

        /// <summary>IsRunning
        /// </summary>
        bool IsRunning { get; }

        /// <summary>Run socket server
        /// </summary>
        ValueTask RunAsync();

        /// <summary>Push message async
        /// </summary>
        ValueTask<ResponsePush> PushAsync(RequestPush request, string sessionId, int timeoutMillis = 5000);

        /// <summary>Push message async to multiple client
        /// </summary>
        ValueTask PushMultipleAsync(RequestPush request, Func<ISocketSession, bool> predicate);

        /// <summary>Server close
        /// </summary>
        ValueTask CloseAsync();

        /// <summary>Register RequestHandler
        /// </summary>
        void RegisterRequestHandler(short code, IRequestMessageHandler handler);

        /// <summary>Register SessionService
        /// </summary>
        void RegisterSessionService(ISessionService sessionService);

        /// <summary>Get sessions
        /// </summary>
        List<ISocketSession> GetSessions(Func<ISocketSession, bool> predicate = null);
    }
}
