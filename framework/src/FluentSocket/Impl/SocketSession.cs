using System;
using System.Net;

namespace FluentSocket
{
    /// <summary>The session of socket
    /// </summary>
    public class SocketSession : ISocketSession
    {
        /// <summary>SessionId
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>Remote EndPoint
        /// </summary>
        public EndPoint RemoteEndPoint { get; set; }

        /// <summary>Local EndPoint
        /// </summary>
        public EndPoint LocalEndPoint { get; set; }

        /// <summary>State
        /// </summary>
        public SocketSessionState State { get; set; } = SocketSessionState.None;

        /// <summary>StartTime
        /// </summary>
        public DateTime StartTime { get; set; }
    }
}
