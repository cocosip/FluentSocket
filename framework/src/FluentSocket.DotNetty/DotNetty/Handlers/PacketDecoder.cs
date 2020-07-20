using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using FluentSocket.Protocols;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.DotNetty.Handlers
{
    public class PacketDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output) => DoDecode(context, message, output);

        internal void DoDecode(IChannelHandlerContext context, IByteBuffer buffer, List<object> output)
        {
            //1 byte type, 4 byte sequence, 4 byte length
            if (buffer.ReadableBytes < 9)
            {
                return;
            }
            var lengthBuffer = buffer.Slice(5, 4);
            // is that right?
            var length = lengthBuffer.GetInt(5);

            // buffer is smaller than packet length
            if (buffer.ReadableBytes < 9 + length)
            {
                return;
            }

            var packetType = (PacketType)buffer.ReadByte();
            var sequence = buffer.ReadInt();
            length = buffer.ReadInt();

            switch (packetType)
            {
                case PacketType.PINGREQ:
                    var pingCode = buffer.ReadByte();
                    var pingPacket = new PingPacket()
                    {
                        Sequence = sequence,
                        PingCode = pingCode
                    };
                    output.Add(pingPacket);
                    break;
                case PacketType.PINGRESP:
                    var pongCode = buffer.ReadByte();
                    var pongPacket = new PongPacket()
                    {
                        Sequence = sequence,
                        PongCode = pongCode
                    };
                    output.Add(pongPacket);
                    break;
                case PacketType.MESSAGEREQ:
                    var code = buffer.ReadShort();
                    var messageReqPacket = new ReqMessagePacket()
                    {
                        Sequence = sequence,
                        Code = code,
                        Body = new byte[length - 2]
                    };
                    buffer.ReadBytes(messageReqPacket.Body);
                    output.Add(messageReqPacket);
                    break;
                case PacketType.MESSAGERESP:
                    var respCode = buffer.ReadShort();
                    var messageRespPacket = new RespMessagePacket()
                    {
                        Sequence = sequence,
                        Code = respCode,
                        Body = new byte[length - 2]
                    };
                    buffer.ReadBytes(messageRespPacket.Body);
                    output.Add(messageRespPacket);
                    break;
                case PacketType.PUSHREQ:
                    var req_PushType = buffer.ReadByte();
                    var reqPush_Code = buffer.ReadShort();
                    var reqPushPacket = new ReqPushPacket()
                    {
                        Sequence = sequence,
                        Code = reqPush_Code,
                        PushType = (PushType)req_PushType,
                        Body = new byte[length - 3]
                    };
                    buffer.ReadBytes(reqPushPacket.Body);
                    output.Add(reqPushPacket);
                    break;
                case PacketType.PUSHRESP:
                    var resp_PushType = buffer.ReadByte();
                    var respPush_Code = buffer.ReadShort();
                    var respPushPacket = new RespPushPacket()
                    {
                        Sequence = sequence,
                        Code = respPush_Code,
                        PushType = (PushType)resp_PushType,
                        Body = new byte[length - 3]
                    };
                    buffer.ReadBytes(respPushPacket.Body);
                    output.Add(respPushPacket);
                    break;

                default:
                    throw new ArgumentException("Invalid packet type!");
            }
        }

        static string DecodeStringInUtf8(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

    }
}
