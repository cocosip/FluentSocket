namespace FluentSocket.Protocols
{
    public class RequestCodes
    {
        public const int Zero = 0;

        public const int HeartBeat = 1;
    }

    public class ResponseCodes
    {
        public const int Zero = 0;

        public const int HeartBeatReply = 2;

        public const int HasException = -1;
    }
}
