namespace FluentSocket.Protocols
{
    public class ResponsePush : Message
    {
        public PushType PushType { get; set; }

        public virtual void ReadBody()
        {

        }
    }
}
