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
        private readonly Action _channelWritabilityChangedHandler;
        public SocketClientHandler(ILogger<SocketClientHandler> logger, Func<PushReqPacket, ValueTask> writePushReqPacketHandler, Action<MessageRespPacket> setMessageRespPacketHandler, Action channelWritabilityChangedHandler)
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
            _logger.LogInformation("Channel '{0}' WritabilityChanged,current is '{1}'", context.Channel.Id.AsLongText(), context.Channel.IsWritable);
            _channelWritabilityChangedHandler();
            context.FireChannelWritabilityChanged();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _logger.LogError(exception, "Channel '{0}' caught some exception,{1}.", context?.Channel.Id.AsLongText());
        }



    }
}
