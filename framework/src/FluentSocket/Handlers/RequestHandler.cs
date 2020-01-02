﻿using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using FluentSocket.Traffic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FluentSocket.Handlers
{
    public class RequestHandler : SimpleChannelInboundHandler<RequestMessage>
    {
        private readonly ManualResetEventSlim _manualResetEventSlim = new ManualResetEventSlim(false);
        private readonly ILogger _logger;
        private IDictionary<int, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ServerSetting _setting;
        public RequestHandler(ILogger<RequestHandler> logger, ServerSetting setting)
        {
            _logger = logger;
            _requestMessageHandlerDict = new Dictionary<int, IRequestMessageHandler>();
            _setting = setting;
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
        protected override void ChannelRead0(IChannelHandlerContext ctx, RequestMessage msg)
        {
            try
            {
                if (_requestMessageHandlerDict.TryGetValue(msg.Code, out IRequestMessageHandler handler))
                {

                    handler.HandleRequestAsync(msg).ContinueWith(t =>
                    {
                        if (t.IsCompleted && t.Exception == null)
                        {
                            WriteAndFlush(ctx, t.Result);
                        }
                        else
                        {
                            var response = ResponseMessage.BuildExceptionResponse(msg, t.Exception.Message);
                            WriteAndFlush(ctx, t.Result);
                        }
                    });
                }
                else
                {
                    _logger.LogError($"Server can't find request code handler! Request code is:{msg.Code}");
                    var response = ResponseMessage.BuildExceptionResponse(msg, $"Server can't find request code handler! Request code is:{msg.Code}");
                    WriteAndFlush(ctx, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(RequestHandler)},There is some error when handle remoting request! Exception:{ex.Message}", ex);
                var response = ResponseMessage.BuildExceptionResponse(msg, $"Server can't find request code handler! Request code is:{msg.Code}");
                WriteAndFlush(ctx, response);
            }
            finally
            {
                ReferenceCountUtil.SafeRelease(msg);
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

        private void WriteAndFlush(IChannelHandlerContext context, ResponseMessage response)
        {
            if (!context.Channel.IsWritable)
            {
                _manualResetEventSlim.Wait();
            }
            context.WriteAndFlushAsync(response);
        }

    }
}
