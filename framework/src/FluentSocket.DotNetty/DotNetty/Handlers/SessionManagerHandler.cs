using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using System;

namespace FluentSocket.DotNetty.Handlers
{
    /// <summary>Channel manager
    /// </summary>
    public class SessionManagerHandler : ChannelHandlerAdapter
    {
        private readonly ILogger _logger;
        private readonly Action<IChannelId> _channelActiveHandler;
        private readonly Action<IChannelId> _channelInactiveHandler;

        public SessionManagerHandler(ILogger<SessionManagerHandler> logger, Action<IChannelId> channelActiveHandler, Action<IChannelId> channelInactiveHandler)
        {
            _logger = logger;
            _channelActiveHandler = channelActiveHandler;
            _channelInactiveHandler = channelInactiveHandler;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            _logger.LogDebug($"Remoting channel active,channel id:{context.Channel.Id.AsShortText()}");
            //Active
            _channelActiveHandler(context.Channel.Id);
            context.FireChannelActive();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _logger.LogDebug($"Remoting channel inactive,channel id :{context.Channel.Id.AsShortText()},localIp:{context.Channel.LocalAddress.ToStringAddress()}");
            //Inactive
            _channelInactiveHandler(context.Channel.Id);
            context.FireChannelInactive();
        }

    }
}
