namespace FluentSocket.Channels
{
    public class ChannelInfo
    {
        public string ChannelId { get; set; }
        public string RemoteIPAddress { get; set; }
        public string Session { get; set; }

        public ChannelInfo()
        {

        }

        public ChannelInfo(string channelId, string remoteIPAddress)
        {
            ChannelId = channelId;
            RemoteIPAddress = remoteIPAddress;
        }

        public ChannelInfo(string channelId, string remoteIPAddress, string session)
        {
            ChannelId = channelId;
            RemoteIPAddress = remoteIPAddress;
            Session = session;
        }

        public override string ToString()
        {
            return $"[ChannelId:{ChannelId},RemoteIPAddress:{RemoteIPAddress},Session:{Session}]";
        }
    }
}
