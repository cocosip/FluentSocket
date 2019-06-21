﻿using System.Threading.Tasks;
using FluentSocket.Codecs;
using FluentSocket.Protocols;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;

namespace FluentSocket.Traffic
{
    public class HeartbeatRequestMessageHander : IRequestMessageHandler
    {
        private readonly ILogger _logger;

        public HeartbeatRequestMessageHander(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
        }



        public Task<ResponseMessage> HandleRequestAsync(RequestMessage request)
        {
            var response = new ResponseMessage(ResponseCodes.HeartBeatReply, ByteUtil.EmptyBytes, request.Id, request.Code, request.CreatedTime);
            _logger.LogDebug("Receive heartbeat async!");
            return Task.FromResult(response);
        }
    }
}
