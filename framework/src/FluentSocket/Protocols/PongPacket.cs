namespace FluentSocket.Protocols
{
    public class PongPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PINGRESP;

        public byte PongCode { get; set; } = 2;
    }
}
