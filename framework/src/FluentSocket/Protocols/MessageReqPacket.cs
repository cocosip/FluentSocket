﻿namespace FluentSocket.Protocols
{
    /// <summary>
    /// 4 byte packet length
    /// 1 byte packet type
    /// 2 byte code
    /// 4 byte body length
    ///  byte[] body
    /// </summary>
    public class MessageReqPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.MESSAGEREQ;
        public short Code { get; set; }
        public byte[] Body { get; set; }
    }
}
