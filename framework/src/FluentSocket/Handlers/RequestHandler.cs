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
    public class RequestHandler : SimpleChannelInboundHandler<RequestMessage>
    {
        private readonly ILogger _logger;
        private IDictionary<int, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ServerSetting _setting;
        public RequestHandler(ILoggerFactory loggerFactory, ServerSetting setting)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _requestMessageHandlerDict = new Dictionary<int, IRequestMessageHandler>();
            _setting = setting;
        }

        //public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        //{
        //    if (context.Channel.IsWritable)
        //    {
        //        context.Flush();
        //    }
        //    base.ChannelWritabilityChanged(context);
        //}
        protected override void ChannelRead0(IChannelHandlerContext ctx, RequestMessage msg)
        {
            try
            {
                if (_requestMessageHandlerDict.TryGetValue(msg.Code, out IRequestMessageHandler handler))
                {

                    //if (_setting.EnableAsyncRequestHandler)
                    //{
                    //    Task.Run(() =>
                    //    {
                    //        if (!ctx.Channel.IsWritable)
                    //        {
                    //            _logger.LogInformation("CurrrentChannel is not writable!");
                    //        }
                    //        var response = handler.HandleRequest(msg);
                    //        ctx.WriteAndFlushAsync(response);
                    //    });
                    //}
                    //else
                    //{
                    //    var response = handler.HandleRequest(msg);
                    //    ctx.WriteAndFlushAsync(response);
                    //}
                    var response = handler.HandleRequest(msg);
                    ctx.WriteAndFlushAsync(response);
                }
                else
                {
                    _logger.LogError($"Server can't find request code handler! Request code is:{msg.Code}");
                    var response = ResponseMessage.BuildExceptionResponse(msg, $"Server can't find request code handler! Request code is:{msg.Code}");
                    ctx.WriteAndFlushAsync(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(RequestHandler)},There is some error when handle remoting request! Exception:{ex.Message}", ex);
                var response = ResponseMessage.BuildExceptionResponse(msg, $"Server can't find request code handler! Request code is:{msg.Code}");
                ctx.WriteAndFlushAsync(response);
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
