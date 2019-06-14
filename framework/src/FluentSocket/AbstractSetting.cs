using DotNetty.Transport.Channels;
using System;
using System.Security.Cryptography.X509Certificates;

namespace FluentSocket
{
    public abstract class AbstractSetting
    {
        /// <summary>Enable ssl
        /// </summary>
        public bool IsSsl { get; set; }

        /// <summary>X509 cert
        /// </summary>
        public X509Certificate2 TlsCertificate { get; set; }

        /// <summary>Round robin request expired interval (ms)
        /// </summary>
        public int ScanTimeoutRequestInterval { get; set; } = 1000;

        /// <summary>Enable heartbeat
        /// </summary>
        public bool EnableHeartbeat { get; set; } = false;

        /// <summary>Heartbeat read time (s)
        /// </summary>
        public int ReaderIdleTimeSeconds { get; set; } = 30;

        /// <summary>Heartbeat write time (s)
        /// </summary>
        public int WriterIdleTimeSeconds { get; set; } = 30;

        /// <summary>Heartbeat all time (s)
        /// </summary>
        public int AllIdleTimeSeconds { get; set; } = 60;

        /// <summary>Wait the handler execute time (ms)
        /// </summary>
        public int WaitHandlerExecuteMilliSeconds { get; set; } = 3000;

        /// <summary>Quiet after connection channel close (ms)
        /// </summary>
        public int QuietPeriodMilliSeconds { get; set; } = 100;

        /// <summary>Timeout when close the channel (s)
        /// </summary>
        public int CloseTimeoutSeconds { get; set; } = 1;

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

        /// <summary>To configure the pipeline
        /// </summary>
        public Action<IChannelPipeline> PipelineConfigure { get; set; }
    }
}
