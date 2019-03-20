using FluentSocket.Protocols;
using FluentSocket.Utils;
using System;

namespace FluentSocket.Codecs
{
    public class ResponseMessage : Message
    {
        public int ResponseCode { get; set; }
        public string RequestId { get; set; }
        public int RequestCode { get; set; }
        public DateTime RequestTime { get; set; }


        public ResponseMessage()
        {

        }

        public ResponseMessage(int responseCode, byte[] body, string requestId, int requestCode, DateTime requestTime) : base(ObjectId.GenerateNewStringId(), MessageType.Response, body, DateTime.Now, null)
        {
            ResponseCode = responseCode;
            RequestId = requestId;
            RequestCode = requestCode;
            RequestTime = requestTime;
        }


        public static ResponseMessage BuildExceptionResponse(RequestMessage requestMessage, string message)
        {
            var response = new ResponseMessage(ResponseCodes.HasException, ByteUtil.EncodeStringInUtf8(message), requestMessage.Id, requestMessage.Code, requestMessage.CreatedTime);
            return response;
        }
    }
}
