namespace FluentSocket.Samples.Common.Services
{
    public class MessageSendOption
    {
        public ClientSetting Setting { get; set; }

        public int Total { get; set; } = 1000000;

        public int SendThread { get; set; } = 3;
    }
}
