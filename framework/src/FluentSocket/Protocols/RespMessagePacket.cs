namespace FluentSocket.Protocols
{
    public class RespMessagePacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.MESSAGERESP;

        public short Code { get; set; }
        public byte[] Body { get; set; }
    }
}
