using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.DotNetty.Handlers
{
    public class PacketEncoder : MessageToMessageEncoder<Packet>
    {
        public PacketEncoder()
        {
        }

        protected override void Encode(IChannelHandlerContext context, Packet packet, List<object> output) => DoEncode(context.Allocator, packet, output);

        public static void DoEncode(IByteBufferAllocator bufferAllocator, Packet packet, List<object> output)
        {
            IByteBuffer buffer = bufferAllocator.Buffer();


            try
            {
                // 1 byte PacketType
                buffer.WriteByte((byte)packet.PacketType);
                // 4 byte Sequence
                buffer.WriteInt(packet.Sequence);

                if (packet is PingPacket pingPacket)
                {
                    buffer.WriteByte(pingPacket.PingCode);
                }
                else if (packet is PongPacket pongPacket)
                {
                    buffer.WriteByte(pongPacket.PongCode);
                }
                else if (packet is MessageReqPacket messageReqPacket)
                {
                    buffer.WriteShort(messageReqPacket.Code);
                    buffer.WriteInt(messageReqPacket.Body.Length);
                    buffer.WriteBytes(messageReqPacket.Body);
                }
                else if (packet is MessageRespPacket messageRespPacket)
                {
                    buffer.WriteShort(messageRespPacket.Code);
                    buffer.WriteInt(messageRespPacket.Body.Length);
                    buffer.WriteBytes(messageRespPacket.Body);
                }
                else if (packet is PushReqPacket pushReqPacket)
                {
                    buffer.WriteByte((byte)pushReqPacket.PushType);
                    buffer.WriteShort(pushReqPacket.Code);
                    buffer.WriteInt(pushReqPacket.Body.Length);
                    buffer.WriteBytes(pushReqPacket.Body);
                }
                else if (packet is PushRespPacket pushRespPacket)
                {
                    buffer.WriteByte((byte)pushRespPacket.PushType);
                    buffer.WriteShort(pushRespPacket.Code);
                    buffer.WriteInt(pushRespPacket.Body.Length);
                    buffer.WriteBytes(pushRespPacket.Body);
                }
                else
                {
                    throw new ArgumentException("Invalid packet!");
                }

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }

        }


        internal static byte[] EncodeStringInUtf8(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

    }
}
