namespace FluentSocket.Protocols
{
    /// <summary>
    /// 4 byte packet length
    /// 1 byte packet type
    /// 1 byte pong code
    /// </summary>
    public class PongPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PINGRESP;

        public byte PongCode { get; set; } = 2;
    }
}
