using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty.Handlers
{
    public class ReqPushPacketHandler : SimpleChannelInboundHandler<ReqPushPacket>
    {
        private readonly Func<ReqPushPacket, ValueTask> _writeReqPushPacketHandler;

        public ReqPushPacketHandler(Func<ReqPushPacket, ValueTask> writeReqPushPacketHandler)
        {
            _writeReqPushPacketHandler = writeReqPushPacketHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ReqPushPacket msg)
        {
            _writeReqPushPacketHandler.Invoke(msg);
            //ctx.FireChannelRead(msg);
        }
    }
}
