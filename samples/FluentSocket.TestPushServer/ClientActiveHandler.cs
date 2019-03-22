using DotNetty.Transport.Channels;
using System;

namespace FluentSocket.TestPushServer
{
    public class ClientActiveHandler : ChannelHandlerAdapter
    {
        private readonly Action _startPush;
        public ClientActiveHandler(Action startPush)
        {
            _startPush = startPush;
        }


        public override void ChannelActive(IChannelHandlerContext context)
        {
            //激活
            _startPush.Invoke();
            context.FireChannelActive();
        }
    }
}
