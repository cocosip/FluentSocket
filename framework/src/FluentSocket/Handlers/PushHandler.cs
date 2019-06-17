using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using FluentSocket.Traffic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.Handlers
{
    public class PushHandler : SimpleChannelInboundHandler<PushMessage>
    {
        private readonly ManualResetEventSlim _manualResetEventSlim = new ManualResetEventSlim(false);
        private readonly ILogger _logger;
        private IDictionary<int, IPushMessageHandler> _pushMessageHandlerDict;
        private readonly ClientSetting _setting;

        public PushHandler(ILoggerFactory loggerFactory, ClientSetting setting)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _setting = setting;
            _pushMessageHandlerDict = new Dictionary<int, IPushMessageHandler>();
        }

        public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            if (context.Channel.IsWritable)
            {
                _manualResetEventSlim.Set();
            }
            else
            {
                _manualResetEventSlim.Reset();
            }
            base.ChannelWritabilityChanged(context);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, PushMessage msg)
        {
            try
            {

                if (_pushMessageHandlerDict.TryGetValue(msg.Code, out IPushMessageHandler handler))
                {
                    _logger.LogDebug($"Handle ServerPushMessage,Code is :{msg.Code},PushMessageId:{msg.Id}");

                    if (_setting.EnableAsyncPushHandler)
                    {
                        Task.Run(() =>
                        {
                            var pushResponseMessageTask = handler.HandlePushMessageAsync(msg);
                            pushResponseMessageTask.ContinueWith(t =>
                            {
                                if (t.Exception == null && t.IsCompleted)
                                {
                                    WriteAndFlush(ctx, t.Result);
                                }
                            }, CancellationToken.None);
                        });
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            var pushResponseMessage = handler.HandlePushMessage(msg);
                            WriteAndFlush(ctx, pushResponseMessage);
                        });
                    }
                }
                else
                {
                    _logger.LogError("Client can't find push message handler!");
                    var pushResponseMessage = PushResponseMessage.BuildExceptionPushResponse(msg, "Client can't find push message handler");
                    WriteAndFlush(ctx, pushResponseMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(PushHandler)},There is some error when handle server push message! Exception:{ex.Message}", ex);
                var pushResponseMessage = PushResponseMessage.BuildExceptionPushResponse(msg, ex.Message);
                WriteAndFlush(ctx, pushResponseMessage);
            }
            finally
            {
                ReferenceCountUtil.SafeRelease(msg);
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


        private void WriteAndFlush(IChannelHandlerContext context, PushResponseMessage response)
        {
            if (!context.Channel.IsWritable)
            {
                _manualResetEventSlim.Wait();
            }
            context.WriteAndFlushAsync(response);
        }
    }
}
