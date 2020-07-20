using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty.Handlers
{
    public class SocketServerHandler : SimpleChannelInboundHandler<Packet>
    {
        private readonly ILogger _logger;
        private readonly Func<string, MessageReqPacket, ValueTask> _writeMessageReqPacketHandler;
        private readonly Action<PushRespPacket> _setPushRespPacketHandler;
        private readonly Action<IChannel, bool> _activeInActiveHandler;
        public SocketServerHandler(ILogger<SocketServerHandler> logger, Func<string, MessageReqPacket, ValueTask> writeReqMessagePacketHandler, Action<PushRespPacket> setPushRespPacketHandler, Action<IChannel, bool> activeInActiveHandler)
        {
            _logger = logger;
            _writeMessageReqPacketHandler = writeReqMessagePacketHandler;
            _setPushRespPacketHandler = setPushRespPacketHandler;

            _activeInActiveHandler = activeInActiveHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            //Received 'ReqMessagePacket' from client
            if (msg is MessageReqPacket messageReqPacket)
            {
                _writeMessageReqPacketHandler(ctx.Channel.Id.AsLongText(), messageReqPacket).AsTask().Wait();
            }
            else if (msg is PushRespPacket pushRespPacket)
            {
                _setPushRespPacketHandler(pushRespPacket);
            }
            else
            {
                ctx.FireChannelRead(msg);
            }
        }


        public override void ChannelActive(IChannelHandlerContext context)
        {
            _activeInActiveHandler(context.Channel, true);
            context.FireChannelActive();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _activeInActiveHandler(context.Channel, false);
            context.FireChannelInactive();
        }

    }
}
