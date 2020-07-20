using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty.Handlers
{
    public class ReqPacketHandler : SimpleChannelInboundHandler<ReqMessagePacket>
    {
        private readonly Func<string, ReqMessagePacket, ValueTask> _writeReqPacketHandler;
        public ReqPacketHandler(Func<string, ReqMessagePacket, ValueTask> writeReqPacketHandler)
        {
            _writeReqPacketHandler = writeReqPacketHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ReqMessagePacket msg)
        {
            _writeReqPacketHandler.Invoke(ctx.Channel.Id.AsLongText(), msg);
            ctx.FireChannelRead(msg);
        }
    }
}
