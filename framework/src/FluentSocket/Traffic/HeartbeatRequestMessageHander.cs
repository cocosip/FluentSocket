using FluentSocket.Codecs;
using FluentSocket.Protocols;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public class HeartbeatRequestMessageHander : IRequestMessageHandler
    {
        private readonly ILogger _logger;

        public HeartbeatRequestMessageHander(ILogger<HeartbeatRequestMessageHander> logger)
        {
            _logger = logger;
        }



        public Task<ResponseMessage> HandleRequestAsync(RequestMessage request)
        {
            var response = new ResponseMessage(ResponseCodes.HeartBeatReply, ByteUtil.EmptyBytes, request.Id, request.Code, request.CreatedTime);
            _logger.LogDebug("Receive heartbeat async!");
            return Task.FromResult(response);
        }
    }
}
