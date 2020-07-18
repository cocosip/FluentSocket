namespace FluentSocket.Protocols
{
    public class RequestMessage : Message
    {
        public virtual byte[] GetBody()
        {
            return Body;
        }
    }
}
