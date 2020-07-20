using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;

namespace FluentSocket.DotNetty.Handlers
{
    public class RespPushPacketHandler : SimpleChannelInboundHandler<RespPushPacket>
    {
        private readonly Action<RespPushPacket> _respPushPacketHandler;

        public RespPushPacketHandler(Action<RespPushPacket> respPushPacketHandler)
        {
            _respPushPacketHandler = respPushPacketHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, RespPushPacket msg)
        {
            _respPushPacketHandler(msg);
        }
    }
}
