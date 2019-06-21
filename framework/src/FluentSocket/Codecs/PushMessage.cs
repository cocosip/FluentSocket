namespace FluentSocket.Codecs
{
    public class PushMessage : RequestMessage
    {
        public bool NeedAck { get; set; }

        public PushMessage()
        {

        }

        public PushMessage(short code, byte[] body, bool needAck = true) : base(code, body)
        {
            NeedAck = true;
            MessageType = MessageType.PushRequest;
        }

    }
}
