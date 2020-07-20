namespace FluentSocket.Protocols
{
    public class PushRespPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PUSHRESP;

        public PushType PushType { get; set; }

        public short Code { get; set; }

        public byte[] Body { get; set; }
    }
}
