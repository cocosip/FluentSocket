using DotNetty.Transport.Channels;
using FluentSocket.Protocols;

namespace FluentSocket.DotNetty
{
    public class ReqMessagePacketWrapper
    {
        public IChannelId Id { get; set; }

        public ReqMessagePacket Packet { get; set; }
    }
}
