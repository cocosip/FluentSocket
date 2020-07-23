using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using Microsoft.Extensions.Logging;

namespace FluentSocket.DotNetty.Handlers
{
    /// <summary>Handle PingPacket ,Response PongPacket
    /// </summary>
    public class PingPacketHandler : SimpleChannelInboundHandler<PingPacket>
    {
        private readonly ILogger _logger;

        public PingPacketHandler(ILogger<PingPacketHandler> logger)
        {
            _logger = logger;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, PingPacket msg)
        {
            _logger.LogDebug("Receive Ping-[{0}]", msg.PingCode);
            try
            {
                var pongPacket = new PongPacket()
                {
                    Sequence = 0
                };

                ctx.WriteAndFlushAsync(pongPacket);
            }
            finally
            {
                ReferenceCountUtil.SafeRelease(msg);
            }
        }
    }
}
