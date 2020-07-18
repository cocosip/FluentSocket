﻿using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty.Handlers
{
    public class PacketHandler : SimpleChannelInboundHandler<Packet>
    {
        private readonly Action<RespMessagePacket> _respPacketHandler;
        private readonly Func<ReqMessagePacket, ValueTask> _writeReqPacketHandler;
        public PacketHandler(Action<RespMessagePacket> messageRespPacketHandler, Func<ReqMessagePacket, ValueTask> writeReqMessagePacketHandler)
        {
            _respPacketHandler = messageRespPacketHandler;
            _writeReqPacketHandler = writeReqMessagePacketHandler;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            if (msg is RespMessagePacket messageRespPacket)
            {
                _respPacketHandler(messageRespPacket);
            }
            else if (msg is ReqMessagePacket messageReqPacket)
            {
                _writeReqPacketHandler.Invoke(messageReqPacket);
            }
            else
            {
                ctx.FireChannelRead(msg);
            }
        }
    }
}
