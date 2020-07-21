using FluentSocket.Protocols;
using SuperSocket.ProtoBase;
using System;
using System.Buffers;

namespace FluentSocket.SuperSocket
{
    public class PacketFilter : FixedHeaderPipelineFilter<Packet>
    {
        public PacketFilter(int headerSize) : base(headerSize)
        {
        }

        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
