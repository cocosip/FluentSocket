using DotNetty.Transport.Channels;
using System;

namespace FluentSocket.DotNetty.Handlers
{
    public class ChannelWritabilityChangedHandler : ChannelHandlerAdapter
    {
        private readonly Action<bool> _channelWritabilityChangedHandler;

        public ChannelWritabilityChangedHandler(Action<bool> channelWritabilityChangedHandler)
        {
            _channelWritabilityChangedHandler = channelWritabilityChangedHandler;
        }

        public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            base.ChannelWritabilityChanged(context);
            _channelWritabilityChangedHandler(context.Channel.IsWritable);
        }
    }
}
