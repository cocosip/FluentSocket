namespace FluentSocket.Protocols
{
    public class RequestPush : Message
    {
        public PushType PushType { get; set; }

        public virtual byte[] GetBody()
        {
            return Body;
        }
    }
}
