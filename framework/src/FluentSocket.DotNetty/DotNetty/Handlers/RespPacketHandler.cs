using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;

namespace FluentSocket.DotNetty.Handlers
{
    public class RespPacketHandler : SimpleChannelInboundHandler<RespMessagePacket>
    {
        private readonly Action<RespMessagePacket> _respPacketHandler;

        public RespPacketHandler(Action<RespMessagePacket> handleRespPacketHandler)
        {
            _respPacketHandler = handleRespPacketHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, RespMessagePacket msg)
        {
            _respPacketHandler(msg);
        }
    }
}
