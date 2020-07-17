using System.Security.Cryptography.X509Certificates;

namespace FluentSocket.DotNetty
{
    public abstract class DotNettyExtraSetting
    {
        /// <summary>Enable ssl
        /// </summary>
        public bool IsSsl { get; set; }

        /// <summary>X509 cert
        /// </summary>
        public X509Certificate2 TlsCertificate { get; set; }

        /// <summary>Write buffer high water 2M
        /// </summary>
        public int WriteBufferHighWaterMark { get; set; } = 1024 * 1024 * 2;

        /// <summary>Write buffer low water 1M
        /// </summary>
        public int WriteBufferLowWaterMark { get; set; } = 1024 * 1024;

        /// <summary>Receive
        /// </summary>
        public int SoRcvbuf { get; set; } = 1024 * 1024;

        /// <summary>Send
        /// </summary>
        public int SoSndbuf { get; set; } = 1024 * 1024;

        /// <summary>Whether write and flush now
        /// </summary>
        public bool TcpNodelay { get; set; } = true;

        /// <summary>Reuse ip address
        /// </summary>
        public bool SoReuseaddr { get; set; } = false;

        /// <summary>AutoRead
        /// </summary>
        public bool AutoRead { get; set; } = true;
    }
}
