namespace FluentSocket.Protocols
{
    public enum PacketType : byte
    {
        PINGREQ = 1,
        PINGRESP = 2,
        MESSAGEREQ = 3,
        MESSAGERESP = 4,

        ERROR = 10
    }
}
