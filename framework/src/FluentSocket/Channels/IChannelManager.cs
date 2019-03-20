using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;

namespace FluentSocket.Channels
{
    public interface IChannelManager
    {
        IChannelGroup Group { get; }
        void Initialize(IChannelGroup channelGroup);
        void AddChannel(IChannel channel);
        void RemoveChannel(Func<ChannelInfo, bool> predicate);
        ChannelInfo FindChannelInfo(Func<ChannelInfo, bool> predicate);
        IChannel FindFirstChannel(Func<ChannelInfo, bool> predicate);
        List<IChannel> FindChannels(Func<ChannelInfo, bool> predicate);
    }
}
