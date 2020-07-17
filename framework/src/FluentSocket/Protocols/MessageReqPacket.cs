namespace FluentSocket.Protocols
{
    public class MessageReqPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.MESSAGEREQ;
    }
}
