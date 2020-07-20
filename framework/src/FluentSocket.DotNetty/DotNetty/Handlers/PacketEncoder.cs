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
                else if (packet is ReqMessagePacket reqMessagePacket)
                {
                    var length = 2 + reqMessagePacket.Body.Length;
                    buffer.WriteInt(length);
                    buffer.WriteShort(reqMessagePacket.Code);
                    buffer.WriteBytes(reqMessagePacket.Body);
                }
                else if (packet is RespMessagePacket respMessagePacket)
                {
                    var length = 2 + respMessagePacket.Body.Length;
                    buffer.WriteInt(length);
                    buffer.WriteShort(respMessagePacket.Code);
                    buffer.WriteBytes(respMessagePacket.Body);
                }
                else if (packet is ReqPushPacket reqPushPacket)
                {
                    var length = 3 + reqPushPacket.Body.Length;
                    buffer.WriteInt(length);
                    buffer.WriteByte((byte)reqPushPacket.PushType);
                    buffer.WriteShort(reqPushPacket.Code);
                    buffer.WriteBytes(reqPushPacket.Body);
                }
                else if (packet is RespPushPacket respPushPacket)
                {
                    var length = 3 + respPushPacket.Body.Length;
                    buffer.WriteInt(length);
                    buffer.WriteByte((byte)respPushPacket.PushType);
                    buffer.WriteShort(respPushPacket.Code);
                    buffer.WriteBytes(respPushPacket.Body);
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
