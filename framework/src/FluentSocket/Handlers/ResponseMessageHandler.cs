using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using Microsoft.Extensions.Logging;
using System;

namespace FluentSocket.Handlers
{
    public class ResponseMessageHandler : SimpleChannelInboundHandler<ResponseMessage>
    {
        private readonly ILogger _logger;
        private Action<ResponseMessage> _setResponseAction;
        public ResponseMessageHandler(ILogger<ResponseMessageHandler> logger, Action<ResponseMessage> setResponseAction)
        {
            _logger = logger;
            _setResponseAction = setResponseAction;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ResponseMessage msg)
        {
            if (msg.MessageType == MessageType.Response)
            {
                _setResponseAction(msg);
                //Task.Run(() => _setResponseAction(msg));
            }
            ctx.FireChannelRead(msg);
        }
    }
}
