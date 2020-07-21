using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using Microsoft.Extensions.Logging;

namespace FluentSocket.DotNetty.Handlers
{
    public class PongPacketHandler : SimpleChannelInboundHandler<PongPacket>
    {
        private readonly ILogger _logger;
        public PongPacketHandler(ILogger<PongPacketHandler> logger)
        {
            _logger = logger;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, PongPacket msg)
        {
            _logger.LogDebug("Receive Pong-[{0}]", msg.PongCode);
            //ReferenceCountUtil.SafeRelease(msg);
        }


    }
}
