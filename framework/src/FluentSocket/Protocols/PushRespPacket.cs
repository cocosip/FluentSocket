namespace FluentSocket.Protocols
{
    /// <summary>
    /// 4 byte packet length
    /// 1 byte packet type
    /// 1 byte push type
    /// 2 byte code
    /// 4 byte body length
    ///  byte[] body
    /// </summary>
    public class PushRespPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PUSHRESP;

        public PushType PushType { get; set; }

        public short Code { get; set; }

        public byte[] Body { get; set; }
    }
}
