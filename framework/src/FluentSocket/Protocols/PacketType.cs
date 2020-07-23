namespace FluentSocket.Protocols
{
    public enum PacketType : byte
    {
        PINGREQ = 1,
        PINGRESP = 2,
        
        //send message
        MESSAGEREQ = 3,
        MESSAGERESP = 4,

        //push message
        PUSHREQ = 5,
        PUSHRESP = 6
    }
}
