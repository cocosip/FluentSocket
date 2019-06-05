using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using FluentSocket.Channels;
using FluentSocket.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace FluentSocket.Handlers
{
    /// <summary>Channel manager
    /// </summary>
    public class ServerChannelManagerHandler : ChannelHandlerAdapter
    {
        private bool _initialized = false;
        private readonly IChannelManager _channelManager;
        private readonly ILogger _logger;
        private readonly Action<ChannelInfo> _onChannelActiveHandler;
        private readonly Action<ChannelInfo> _onChannelInActiveHandler;
        private object SyncObject = new object();
        public ServerChannelManagerHandler(ILoggerFactory loggerFactory, IChannelManager channelManager, Action<ChannelInfo> onChannelActiveHandler, Action<ChannelInfo> onChannelInActiveHandler)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _channelManager = channelManager;
            _onChannelActiveHandler = onChannelActiveHandler;
            _onChannelInActiveHandler = onChannelInActiveHandler;
            _initialized = false;
        }

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            if (!_initialized)
            {
                lock (SyncObject)
                {
                    var group = new DefaultChannelGroup(contex.Executor);
                    _channelManager.Initialize(group);
                    _initialized = true;
                }
            }
            var channelInfo = _channelManager.AddChannel(contex.Channel);
            _logger.LogInformation($"Remoting channel active,channel id:{contex.Channel.Id.AsShortText()}");

            //Add
            _onChannelActiveHandler?.Invoke(channelInfo);
            contex.FireChannelActive();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            var channelInfo = _channelManager.RemoveChannel(c => c.ChannelId == context.Channel.Id.AsLongText());
            _logger.LogInformation($"Remoting channel inactive,channel id :{context.Channel.Id.AsShortText()},localIp:{context.Channel.LocalAddress.ToStringAddress()}");

            //Remove 
            _onChannelInActiveHandler?.Invoke(channelInfo);

            context.FireChannelInactive();
        }

    }
}
