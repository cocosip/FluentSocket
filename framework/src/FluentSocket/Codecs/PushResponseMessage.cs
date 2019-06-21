using FluentSocket.Protocols;
using FluentSocket.Utils;
using System;

namespace FluentSocket.Codecs
{
    public class PushResponseMessage : ResponseMessage
    {
        public PushResponseMessage()
        {

        }


        public PushResponseMessage(short responseCode, byte[] body, string requestId, short requestCode, DateTime requestTime) : base(responseCode, body, requestId, requestCode, requestTime)
        {
            MessageType = MessageType.PushResponse;
        }

        public static PushResponseMessage BuildExceptionPushResponse(PushMessage pushMessage, string message)
        {
            var response = new PushResponseMessage(ResponseCodes.HasException, ByteUtil.EncodeStringInUtf8(message), pushMessage.Id, pushMessage.Code, pushMessage.CreatedTime);
            return response;
        }
    }
}
