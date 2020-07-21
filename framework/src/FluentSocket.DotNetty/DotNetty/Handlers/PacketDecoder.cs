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
            ////1 byte type, 4 byte sequence, 4 byte length
            //if (buffer.ReadableBytes < 9)
            //{
            //    return;
            //}
            ////var lengthBuffer = buffer.Slice(5, 4);
            ////// is that right?
            ////var length = lengthBuffer.ReadInt();

            //var length = buffer.GetInt(5);

            //// buffer is smaller than packet length
            //if (buffer.ReadableBytes < 9 + length)
            //{
            //    return;
            //}

            var packetType = (PacketType)buffer.ReadByte();
            var sequence = buffer.ReadInt();

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
                    var req_code = buffer.ReadShort();
                    var req_length = buffer.ReadInt();
                    var messageReqPacket = new MessageReqPacket()
                    {
                        Sequence = sequence,
                        Code = req_code,
                        Body = new byte[req_length]
                    };
                    buffer.ReadBytes(messageReqPacket.Body);
                    output.Add(messageReqPacket);
                    break;
                case PacketType.MESSAGERESP:
                    var resp_code = buffer.ReadShort();
                    var resp_length = buffer.ReadInt();
                    var messageRespPacket = new MessageRespPacket()
                    {
                        Sequence = sequence,
                        Code = resp_code,
                        Body = new byte[resp_length]
                    };
                    buffer.ReadBytes(messageRespPacket.Body);
                    output.Add(messageRespPacket);
                    break;
                case PacketType.PUSHREQ:
                    var reqPush_type = buffer.ReadByte();
                    var reqPush_code = buffer.ReadShort();
                    var reqPush_length = buffer.ReadInt();
                    var reqPushPacket = new PushReqPacket()
                    {
                        Sequence = sequence,
                        Code = reqPush_code,
                        PushType = (PushType)reqPush_type,
                        Body = new byte[reqPush_length]
                    };
                    buffer.ReadBytes(reqPushPacket.Body);
                    output.Add(reqPushPacket);
                    break;
                case PacketType.PUSHRESP:
                    var respPush_type = buffer.ReadByte();
                    var respPush_Code = buffer.ReadShort();
                    var respPush_length = buffer.ReadInt();
                    var respPushPacket = new PushRespPacket()
                    {
                        Sequence = sequence,
                        Code = respPush_Code,
                        PushType = (PushType)respPush_type,
                        Body = new byte[respPush_length]
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
