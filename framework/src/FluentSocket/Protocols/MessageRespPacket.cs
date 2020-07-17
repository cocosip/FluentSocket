namespace FluentSocket.Protocols
{
    public class MessageRespPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.MESSAGERESP;

    }
}
