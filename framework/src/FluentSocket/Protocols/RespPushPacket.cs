namespace FluentSocket.Protocols
{
    public class RespPushPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PUSHRESP;

        public PushType PushType { get; set; }

        public short Code { get; set; }

        public byte[] Body { get; set; }
    }
}
