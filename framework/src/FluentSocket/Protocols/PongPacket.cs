using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.Protocols
{
    public class PongPacket : Packet
    {
        public override PacketType PacketType { get; set; } = PacketType.PINGRESP;


    }
}
