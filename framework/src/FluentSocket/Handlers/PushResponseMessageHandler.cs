using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using Microsoft.Extensions.Logging;
using System;

namespace FluentSocket.Handlers
{
    public class PushResponseMessageHandler : SimpleChannelInboundHandler<PushResponseMessage>
    {
        private readonly ILogger _logger;
        private Action<PushResponseMessage> _setPushResponseMessageAction;

        public PushResponseMessageHandler(ILoggerFactory loggerFactory, Action<PushResponseMessage> setPushResponseMessageAction)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _setPushResponseMessageAction = setPushResponseMessageAction;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, PushResponseMessage msg)
        {
            if (msg.MessageType == MessageType.PushResponse)
            {
                _setPushResponseMessageAction(msg);
            }
            ctx.FireChannelRead(msg);
        }
    }
}
