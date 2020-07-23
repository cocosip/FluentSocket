using System.Net;

namespace FluentSocket
{
    public class ServerSetting : Setting
    {
        /// <summary>Listening ip address and port
        /// </summary>
        public IPEndPoint ListeningEndPoint { get; set; }

        /// <summary>'MessageReqPacket' channel capacity
        /// </summary>
        public int MessageReqCapacity { get; set; } = 20000;

        /// <summary>Thread count of handle 'MessageReqPacket'
        /// </summary>
        public int HandleMessageReqThread { get; set; } = 1;

    }
}
