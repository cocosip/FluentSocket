namespace FluentSocket.Protocols
{
    public class ReqPushPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PUSHREQ;

        public PushType PushType { get; set; }

        public short Code { get; set; }

        public byte[] Body { get; set; }

    }
}
