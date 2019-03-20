using FluentSocket.Utils;
using System;

namespace FluentSocket.Codecs
{
    public class RequestMessage : Message
    {
        public int Code { get; set; }

        public RequestMessage()
        {

        }

        public RequestMessage(int code, byte[] body) : base(ObjectId.GenerateNewStringId(), MessageType.Request, body, DateTime.Now, null)
        {
            Code = code;
        }

    }

}
