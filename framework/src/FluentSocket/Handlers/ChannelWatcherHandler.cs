using DotNetty.Transport.Channels;
using System;

namespace FluentSocket.Handlers
{

    public class ChannelWatcherHandler : ChannelHandlerAdapter
    {
        private Action<bool> _channelWritableChangeAction;

        public ChannelWatcherHandler(Action<bool> channelWritableChangeAction)
        {
            _channelWritableChangeAction = channelWritableChangeAction;
        }

        public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            base.ChannelWritabilityChanged(context);
            _channelWritableChangeAction(context.Channel.IsWritable);
        }
    }
}
