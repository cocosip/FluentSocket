namespace FluentSocket.Protocols
{
    public class ErrorPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.ERROR;

        public short Code { get; set; }
    }
}
