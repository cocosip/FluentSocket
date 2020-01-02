using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FluentSocket.Handlers
{
    /// <summary>Client reconnect when connection shutdown
    /// </summary>
    public class ReConnectHandler : ChannelHandlerAdapter
    {
        private readonly ILogger _logger;
        private readonly ClientSetting _setting;
        private readonly Func<Task> _reConnectAction;
        public ReConnectHandler(ILogger<ReConnectHandler> logger, ClientSetting setting, Func<Task> reConnectAction)
        {
            _logger = logger;
            _setting = setting;
            _reConnectAction = reConnectAction;
        }


        //public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        //{
        //    base.ExceptionCaught(context, exception);
        //    _logger.LogError($"There is some error in client.{exception.Message}", exception);
        //    ChannelInactive(context);
        //}


        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _logger.LogInformation("Channel:{0} is inactive!", context.Channel.Id.AsLongText());
            context.Channel.EventLoop.Schedule(() => _reConnectAction(), TimeSpan.FromSeconds(_setting.ReConnectDelaySeconds));
        }
    }
}
