using FluentSocket.Codecs;
using FluentSocket.Protocols;
using FluentSocket.Utils;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public class HeartbeatRequestMessageHander : IRequestMessageHandler
    {
        public Task<ResponseMessage> HandleRequestAsync(RequestMessage request)
        {
            var response = new ResponseMessage(ResponseCodes.HeartBeatReply, ByteUtil.EmptyBytes, request.Id, request.Code, request.CreatedTime);
            return Task.FromResult(response);
        }
    }
}
