using FluentSocket.Protocols;

namespace FluentSocket.DotNetty
{
    public class MessageReqPacketWrapper
    {
        public ISocketSession Session { get; set; }

        public MessageReqPacket Packet { get; set; }
    }
}
