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
    public class RequestHandler : SimpleChannelInboundHandler<RequestMessage>
    {
        private IChannelHandlerContext _ctx;
        private readonly ILogger _logger;
        private IDictionary<int, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly AbstractSetting _setting;
        public RequestHandler(ILoggerFactory loggerFactory, AbstractSetting setting)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _requestMessageHandlerDict = new Dictionary<int, IRequestMessageHandler>();
            _setting = setting;
        }
        public override void HandlerAdded(IChannelHandlerContext context) => this._ctx = context;

        protected override void ChannelRead0(IChannelHandlerContext ctx, RequestMessage msg)
        {
            try
            {
                if (_requestMessageHandlerDict.TryGetValue(msg.Code, out IRequestMessageHandler handler))
                {
                    void action()
                    {
                        handler.HandleRequestAsync(msg).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogError($"Handle remotingRequest has error,{t.Exception.Message}", t.Exception);
                            }
                            if (_ctx.Channel.IsWritable)
                            {
                                _ctx.WriteAndFlushAsync(t.Result);
                            }
                            else
                            {
                                _ctx.Flush();
                                ReferenceCountUtil.Release(msg);
                                _logger.LogInformation("Server channel response write,channel is not writable!");
                            }
                        });
                    }
                    //ThreadPool.QueueUserWorkItem(o => action());
                    Task.Run(() => action());
                }
                else
                {
                    _logger.LogError($"Server can't find request code handler! Request code is:{msg.Code}");
                    if (ctx.Channel.IsWritable)
                    {
                        var response = ResponseMessage.BuildExceptionResponse(msg, $"Server can't find request code handler! Request code is:{msg.Code}");
                        ctx.WriteAndFlushAsync(response);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(RequestHandler)},There is some error when handle remoting request! Exception:{ex.Message}", ex);
                if (ctx.Channel.IsWritable)
                {
                    var response = ResponseMessage.BuildExceptionResponse(msg, $"There is some error when handle remoting request! Exception:{ex.Message}");
                    ctx.WriteAndFlushAsync(response);
                }
            }
        }

        /// <summary>Add requestCodeHandler to dict
        /// </summary>
        public RequestHandler RegisterRequestHandler(int code, IRequestMessageHandler requestMessageHandler)
        {
            _requestMessageHandlerDict.Add(code, requestMessageHandler);
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
