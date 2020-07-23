using FluentSocket.Protocols;
using SuperSocket.ProtoBase;
using System;
using System.Buffers;

namespace FluentSocket.SuperSocket
{
    public class PacketFilter : FixedHeaderPipelineFilter<Packet>
    {

        public PacketFilter() : this(5)
        {

        }

        public PacketFilter(int headerSize) : base(headerSize)
        {
        }

        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }

        protected override Packet DecodePackage(ref ReadOnlySequence<byte> buffer)
        {
            return base.DecodePackage(ref buffer);
        }
    }
}
