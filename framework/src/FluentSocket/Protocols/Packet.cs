namespace FluentSocket.Protocols
{
    public abstract class Packet
    {
        public virtual PacketType PacketType { get; set; }

        public virtual int Sequence { get; set; }

    }
}
