using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty.Handlers
{
    public class SocketClientHandler : SimpleChannelInboundHandler<Packet>
    {
        private readonly ILogger _logger;
        private readonly Func<PushReqPacket, ValueTask> _writePushReqPacketHandler;
        private readonly Action<MessageRespPacket> _setMessageRespPacketHandler;
        private readonly Action<bool> _channelWritabilityChangedHandler;
        public SocketClientHandler(ILogger<SocketClientHandler> logger, Func<PushReqPacket, ValueTask> writePushReqPacketHandler, Action<MessageRespPacket> setMessageRespPacketHandler, Action<bool> channelWritabilityChangedHandler)
        {
            _logger = logger;
            _writePushReqPacketHandler = writePushReqPacketHandler;
            _setMessageRespPacketHandler = setMessageRespPacketHandler;
            _channelWritabilityChangedHandler = channelWritabilityChangedHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            if (msg is MessageRespPacket messageRespPacket)
            {
                _setMessageRespPacketHandler(messageRespPacket);
            }
            else if (msg is PushReqPacket pushReqPacket)
            {
                _writePushReqPacketHandler(pushReqPacket);
            }
            else
            {
                ctx.FireChannelRead(msg);
            }
        }


        public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            _channelWritabilityChangedHandler(context.Channel.IsWritable);
        }


    }
}
