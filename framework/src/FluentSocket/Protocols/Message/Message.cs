namespace FluentSocket.Protocols
{
    public abstract class Message
    {
        public virtual short Code { get; set; }

        public virtual byte[] Body { get; set; }

    }
}
