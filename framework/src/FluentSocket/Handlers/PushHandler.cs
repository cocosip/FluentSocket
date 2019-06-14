using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using FluentSocket.Traffic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluentSocket.Handlers
{
    public class PushHandler : SimpleChannelInboundHandler<PushMessage>
    {
        private readonly ILogger _logger;
        private IDictionary<int, IPushMessageHandler> _pushMessageHandlerDict;
        private readonly ClientSetting _setting;

        public PushHandler(ILoggerFactory loggerFactory, ClientSetting setting)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _setting = setting;
            _pushMessageHandlerDict = new Dictionary<int, IPushMessageHandler>();
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, PushMessage msg)
        {
            try
            {

                if (_pushMessageHandlerDict.TryGetValue(msg.Code, out IPushMessageHandler handler))
                {
                    _logger.LogDebug($"Handle ServerPushMessage,Code is :{msg.Code},PushMessageId:{msg.Id}");

                    void action()
                    {
                        handler.HandlePushMessageAsync(msg).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogError("Handle server push message has error,{0}", t.Exception.Message);
                            }
                            if (ctx.Channel.IsWritable)
                            {
                                ctx.WriteAndFlushAsync(t.Result);
                            }
                            else
                            {
                                _logger.LogInformation("Client channel server push message response write fail,channel is not writable!");
                                ReferenceCountUtil.Release(msg);
                            }
                        });
                    }
                    if (_setting.EnableAsyncPushHandler)
                    {
                        Task.Run(() => action());
                    }
                    else
                    {
                        action();
                    }
                }
                else
                {
                    _logger.LogError("Client can't find push message handler!");
                    if (ctx.Channel.IsWritable)
                    {
                        var pushResponseMessage = PushResponseMessage.BuildExceptionPushResponse(msg, "Client can't find push message handler");
                        ctx.WriteAndFlushAsync(pushResponseMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PushHandler)},There is some error when handle server push message! Exception:{ex.Message}", ex);
                if (ctx.Channel.IsWritable)
                {
                    var pushResponseMessage = PushResponseMessage.BuildExceptionPushResponse(msg, ex.Message);
                    ctx.WriteAndFlushAsync(pushResponseMessage);
                }
            }
        }

        /// <summary>Add push handler to dict.
        /// </summary>
        public PushHandler RegisterPushMessageHandler(int code, IPushMessageHandler pushMessageHandler)
        {
            _pushMessageHandlerDict.Add(code, pushMessageHandler);
            return this;
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _logger.LogError(exception.Message, exception);
            context.CloseAsync();
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
    }
}
