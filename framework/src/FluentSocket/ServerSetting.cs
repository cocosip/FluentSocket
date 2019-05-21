using FluentSocket.Traffic;
using System;
using System.Collections.Generic;
using System.Net;

namespace FluentSocket
{
    public class ServerSetting : AbstractSetting
    {

        /// <summary>BossGroup循环数
        /// </summary>
        public int BossGroupEventLoopCount { get; set; } = 1;

        /// <summary>WorkGroup循环数
        /// </summary>
        public int WorkGroupEventLoopCount { get; set; } = 1;

        /// <summary>SoBacklog
        /// </summary>
        public int SoBacklog { get; set; } = 128;

        /// <summary>Listening ip address and port
        /// </summary>
        public IPEndPoint ListeningEndPoint { get; set; }

        /// <summary>RequestMessageHandler configure
        /// </summary>
        public Action<IDictionary<int, IRequestMessageHandler>> RequestMessageHandlerConfigure { get; set; }
    }
}
