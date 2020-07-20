using System.Net;

namespace FluentSocket.DotNetty
{
    public struct ChannelInfo
    {
        public string ChannelId { get; set; }

        public EndPoint RemoteEndPoint { get; set; }

        public EndPoint LocalEndPoint { get; set; }

        public ChannelInfo(string channelId, EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            ChannelId = channelId;
            RemoteEndPoint = remoteEndPoint;
            LocalEndPoint = localEndPoint;
        }
    }
}
