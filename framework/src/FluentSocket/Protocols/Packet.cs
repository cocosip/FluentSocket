namespace FluentSocket.Protocols
{
    public abstract class Packet
    {
        public PacketType PacketType { get; set; }

        public int Sequence { get; set; }

        public byte[] Body { get; set; }
    }
}
