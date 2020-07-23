using DotNetty.Common.Utilities;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using FluentSocket.Protocols;

namespace FluentSocket.DotNetty.Handlers
{
    public class HeartbeatHandler : ChannelHandlerAdapter
    {
        public HeartbeatHandler()
        {

        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            if (evt is IdleStateEvent idleStateEvent)
            {
                if (idleStateEvent.State == IdleState.AllIdle)
                {
                    var pingPacket = new PingPacket();
                    //release?
                    ReferenceCountUtil.SafeRelease(idleStateEvent);
                    context.WriteAndFlushAsync(pingPacket);
                }
            }
        }
    }
}
