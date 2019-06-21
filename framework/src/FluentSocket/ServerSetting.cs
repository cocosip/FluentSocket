using FluentSocket.Channels;
using FluentSocket.Traffic;
using System;
using System.Collections.Generic;
using System.Net;

namespace FluentSocket
{
    public class ServerSetting : AbstractSetting
    {
        /// <summary>Enable libuv
        /// </summary>
        public bool UseLibuv { get; set; }

        /// <summary>BossGroup
        /// </summary>
        public int BossGroupEventLoopCount { get; set; } = 1;

        /// <summary>WorkGroup
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

        /// <summary>The action when channel active
        /// </summary>
        public Action<ChannelInfo> OnChannelActiveHandler { get; set; }

        /// <summary>The action when channel inactive
        /// </summary>
        public Action<ChannelInfo> OnChannelInActiveHandler { get; set; }
    }
}
