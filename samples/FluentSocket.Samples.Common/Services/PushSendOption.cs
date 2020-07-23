namespace FluentSocket.Samples.Common.Services
{
    public class PushSendOption
    {
        public ServerSetting Setting { get; set; }
        public int Total { get; set; } = 1000000;

        public int PushThread { get; set; } = 1;

    }
}
