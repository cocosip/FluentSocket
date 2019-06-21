using FluentSocket.Utils;
using System;

namespace FluentSocket.Codecs
{
    public class RequestMessage : Message
    {
        public short Code { get; set; }

        public RequestMessage()
        {

        }

        public RequestMessage(short code, byte[] body) : base(ObjectId.GenerateNewStringId(), MessageType.Request, body, DateTime.Now, null)
        {
            Code = code;
        }

    }

}
