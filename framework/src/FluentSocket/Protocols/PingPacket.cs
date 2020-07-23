namespace FluentSocket.Protocols
{
    /// <summary>
    /// 4 byte packet length
    /// 1 byte packet type
    /// 1 byte ping code
    /// </summary>
    public class PingPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PINGREQ;

        public byte PingCode { get; set; } = 1;
    }
}
