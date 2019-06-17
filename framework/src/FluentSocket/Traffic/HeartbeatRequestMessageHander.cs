using FluentSocket.Codecs;
using FluentSocket.Protocols;
using FluentSocket.Utils;

namespace FluentSocket.Traffic
{
    public class HeartbeatRequestMessageHander : BaseRequestMessageHandler
    {
        public override ResponseMessage HandleRequest(RequestMessage request)
        {
            var response = new ResponseMessage(ResponseCodes.HeartBeatReply, ByteUtil.EmptyBytes, request.Id, request.Code, request.CreatedTime);
            return response;
        }
    }
}
