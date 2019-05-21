using FluentSocket.Traffic;
using System;
using System.Collections.Generic;
using System.Net;

namespace FluentSocket
{
    public class ClientSetting : AbstractSetting
    {
        /// <summary>Group循环数
        /// </summary>
        public int GroupEventLoopCount { get; set; } = 1;

        /// <summary>Server ip address and port
        /// </summary>
        public IPEndPoint ServerEndPoint { get; set; }

        /// <summary>Local bind ip address and port
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }


        /// <summary>PushHandler configure
        /// </summary>
        public Action<IDictionary<int, IPushMessageHandler>> PushMessageHandlerConfigure { get; set; }
    }
}
