﻿using System.Net;

namespace FluentSocket
{
    public class ServerSetting : AbstractSetting
    {
        /// <summary>Listening ip address and port
        /// </summary>
        public IPEndPoint ListeningEndPoint { get; set; }
    }
}
