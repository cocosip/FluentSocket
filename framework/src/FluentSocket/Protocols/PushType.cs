namespace FluentSocket.Protocols
{
    public enum PushType : byte
    {
        Unknow = 0,

        /// <summary>Should not wait any response from client
        /// </summary>
        NoReply = 1,

        /// <summary>Reply from client
        /// </summary>
        Reply = 2
    }
}
