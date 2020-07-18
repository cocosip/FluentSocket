namespace FluentSocket.Protocols
{
    public class PingPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PINGREQ;

        public byte PingCode { get; set; } = 1;
    }
}
