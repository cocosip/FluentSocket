using FluentSocket.Traffic;
using System;
using System.Collections.Generic;
using System.Net;

namespace FluentSocket
{
    public class ClientSetting : AbstractSetting
    {
        /// <summary>Group
        /// </summary>
        public int GroupEventLoopCount { get; set; } = 1;

        /// <summary>Server ip address and port
        /// </summary>
        public IPEndPoint ServerEndPoint { get; set; }

        /// <summary>Local bind ip address and port
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }


        /// <summary>Enable async to handle push handler
        /// </summary>
        public bool EnableAsyncPushHandler { get; set; } = true;

        /// <summary>Keep alive
        /// </summary>
        public bool SoKeepalive { get; set; } = false;

        /// <summary>Enable client to reConnect the server
        /// </summary>
        public bool EnableReConnect { get; set; } = false;

        /// <summary>ReConnect delay seconds (s)
        /// </summary>
        public int ReConnectDelaySeconds { get; set; } = 2;

        /// <summary>ReConnect interval
        /// </summary>
        public int ReConnectIntervalMilliSeconds { get; set; } = 1000;

        /// <summary>Try reConnect max count
        /// </summary>
        public int ReConnectMaxCount { get; set; } = 10;

        /// <summary>PushHandler configure
        /// </summary>
        public Action<IDictionary<int, IPushMessageHandler>> PushMessageHandlerConfigure { get; set; }
    }
}
