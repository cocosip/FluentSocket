using System;
using System.Net;

namespace FluentSocket
{
    /// <summary>The session of socket
    /// </summary>
    public interface ISocketSession
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
        SocketSessionState State { get; set; }

        /// <summary>StartTime
        /// </summary>
        DateTime StartTime { get; set; }
    }
}
