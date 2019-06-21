namespace FluentSocket.Codecs
{
    public enum MessageType : byte
    {
        Request = 1,

        Response = 2,

        PushRequest = 3,

        PushResponse = 4
    }
}
