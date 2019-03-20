using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using FluentSocket.Channels;
using FluentSocket.Extensions;
using Microsoft.Extensions.Logging;

namespace FluentSocket.Handlers
{
    /// <summary>Channel manager
    /// </summary>
    public class ServerChannelManagerHandler : ChannelHandlerAdapter
    {
        private bool _initialized = false;
        private readonly IChannelManager _channelManager;
        private readonly ILogger _logger;
        private object SyncObject = new object();
        public ServerChannelManagerHandler(ILoggerFactory loggerFactory, IChannelManager channelManager)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _channelManager = channelManager;
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
            _channelManager.AddChannel(contex.Channel);
            _logger.LogInformation($"Remoting channel active,channel id:{contex.Channel.Id.AsShortText()}");
            contex.FireChannelActive();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _channelManager.RemoveChannel(c => c.ChannelId == context.Channel.Id.AsLongText());
            _logger.LogInformation($"Remoting channel inactive,channel id :{context.Channel.Id.AsShortText()},localIp:{context.Channel.LocalAddress.ToStringAddress()}");
            context.FireChannelInactive();
        }

    }
}
