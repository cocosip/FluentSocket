using System.Net;

namespace FluentSocket
{
    public class ClientSetting : Setting
    {
        /// <summary>Server ip address and port
        /// </summary>
        public IPEndPoint ServerEndPoint { get; set; }

        /// <summary>Local bind ip address and port (default is null)
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }

        /// <summary>Enable client to reConnect the server
        /// </summary>
        public bool EnableReConnect { get; set; } = false;

        /// <summary>ReConnect delay seconds (default is 3s)
        /// </summary>
        public int ReConnectDelaySeconds { get; set; } = 3;

        /// <summary>Try reConnect max count
        /// </summary>
        public int ReConnectMaxCount { get; set; } = 10;

        /// <summary>Enable heartbeat (default is false)
        /// </summary>
        public bool EnableHeartbeat { get; set; } = false;

        /// <summary>Send heartbeat interval (default is 30s)
        /// </summary>
        public int HeartbeatInterval { get; set; } = 30;
    }
}
