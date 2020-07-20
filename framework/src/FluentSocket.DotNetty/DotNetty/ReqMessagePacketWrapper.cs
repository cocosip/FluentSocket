using FluentSocket.Protocols;

namespace FluentSocket.DotNetty
{
    public class ReqMessagePacketWrapper
    {
        public ISocketSession Session { get; set; }

        public ReqMessagePacket Packet { get; set; }
    }
}
