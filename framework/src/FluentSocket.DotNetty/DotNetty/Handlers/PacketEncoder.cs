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
                    buffer.WriteInt(1);
                    buffer.WriteByte(pingPacket.PingCode);
                }
                else if (packet is PongPacket pongPacket)
                {
                    buffer.WriteInt(1);
                    buffer.WriteByte(pongPacket.PongCode);
                }
                else if (packet is ReqMessagePacket messageReqPacket)
                {
                    var length = 2 + messageReqPacket.Body.Length;
                    buffer.WriteInt(length);
                    buffer.WriteShort(messageReqPacket.Code);
                    buffer.WriteBytes(messageReqPacket.Body);
                }
                else if (packet is RespMessagePacket messageRespPacket)
                {
                    var length = 2 + messageRespPacket.Body.Length;
                    buffer.WriteInt(length);
                    buffer.WriteShort(messageRespPacket.Code);
                    buffer.WriteBytes(messageRespPacket.Body);
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
