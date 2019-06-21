using DotNetty.Common.Utilities;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using FluentSocket.Protocols;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;

namespace FluentSocket.Handlers
{
    public class HeartbeatHandler : ChannelHandlerAdapter
    {
        private readonly ILogger _logger;
        public HeartbeatHandler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
        }


        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            if (evt is IdleStateEvent)
            {
                var idleStateEvent = (IdleStateEvent)evt;
                if (idleStateEvent.State == IdleState.AllIdle)
                {
                    _logger.LogDebug($"Heartbeat, event type:{evt.GetType()},status:{idleStateEvent.State}");
                    var heartbeat = new RequestMessage(RequestCodes.HeartBeat, ByteUtil.EmptyBytes);
                    //release?
                    ReferenceCountUtil.Release(idleStateEvent);
                    context.WriteAndFlushAsync(heartbeat);
                }

            }
        }
    }
}
