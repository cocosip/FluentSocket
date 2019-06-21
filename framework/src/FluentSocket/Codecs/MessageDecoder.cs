using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.Codecs
{
    public class MessageDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output) => DoDecode(context, message, output);


        internal void DoDecode(IChannelHandlerContext context, IByteBuffer buffer, List<object> output)
        {
            var messageType = (MessageType)buffer.ReadByte();
            switch (messageType)
            {
                case MessageType.Request:
                    DecodeRequestMessage(messageType, buffer, output);
                    break;
                case MessageType.Response:
                    DecodeResponseMessage(messageType, buffer, output);
                    break;
                case MessageType.PushRequest:
                    DecodePushMessage(messageType, buffer, output);
                    break;
                case MessageType.PushResponse:
                    DecodePushResponseMessage(messageType, buffer, output);
                    break;
                default:
                    throw new ArgumentException("Unknown decode message type: " + messageType);
            }
        }


        /// <summary>Decode RequestMessage <see cref="FluentSocket.Codecs.RequestMessage" />
        /// </summary>
        static void DecodeRequestMessage(MessageType messageType, IByteBuffer buffer, List<object> output)
        {
            var requestMessage = new RequestMessage()
            {
                MessageType = MessageType.Request
            };
            //Id
            var idLength = buffer.ReadInt();
            byte[] idBytes = new byte[idLength];
            buffer.ReadBytes(idBytes);
            //Id read
            requestMessage.Id = DecodeStringInUtf8(idBytes);
            //code
            requestMessage.Code = buffer.ReadShort();
            //body
            var bodyLength = buffer.ReadInt();
            byte[] body = new byte[bodyLength];
            buffer.ReadBytes(body);
            requestMessage.Body = body;
            //create time
            requestMessage.CreatedTime = new DateTime(buffer.ReadLong());
            //extra
            var extraLength = buffer.ReadInt();
            byte[] extraBytes = new byte[extraLength];
            buffer.ReadBytes(extraBytes);
            requestMessage.Extra = extraBytes;
            output.Add(requestMessage);
        }

        /// <summary>Decode ResponseMessage <see cref="FluentSocket.Codecs.ResponseMessage" />
        /// </summary>
        static void DecodeResponseMessage(MessageType messageType, IByteBuffer buffer, List<object> output)
        {
            var responseMessage = new ResponseMessage()
            {
                MessageType = MessageType.Response
            };

            //Id
            var idLength = buffer.ReadInt();
            byte[] idBytes = new byte[idLength];
            buffer.ReadBytes(idBytes);
            responseMessage.Id = DecodeStringInUtf8(idBytes);
            //code
            responseMessage.ResponseCode = buffer.ReadShort();
            //body
            var bodyLength = buffer.ReadInt();
            byte[] body = new byte[bodyLength];
            buffer.ReadBytes(body);
            responseMessage.Body = body;
            //create time
            responseMessage.CreatedTime = new DateTime(buffer.ReadLong());
            //extra
            var extraLength = buffer.ReadInt();
            byte[] extraBytes = new byte[extraLength];
            buffer.ReadBytes(extraBytes);
            responseMessage.Extra = extraBytes;

            //response.requestId
            var requestIdLength = buffer.ReadInt();
            byte[] requestIdBytes = new byte[requestIdLength];
            buffer.ReadBytes(requestIdBytes);
            responseMessage.RequestId = DecodeStringInUtf8(requestIdBytes);
            //response.requestCode
            responseMessage.RequestCode = buffer.ReadShort();
            //response.requestTime
            responseMessage.RequestTime = new DateTime(buffer.ReadLong());

            output.Add(responseMessage);
        }

        /// <summary>Decode PushMessage <see cref="FluentSocket.Codecs.PushMessage" />
        /// </summary>
        static void DecodePushMessage(MessageType messageType, IByteBuffer buffer, List<object> output)
        {
            var pushMessage = new PushMessage()
            {
                MessageType = MessageType.PushRequest
            };
            //Id
            var idLength = buffer.ReadInt();
            byte[] idBytes = new byte[idLength];
            buffer.ReadBytes(idBytes);
            //Id read
            pushMessage.Id = DecodeStringInUtf8(idBytes);
            //code
            pushMessage.Code = buffer.ReadShort();
            //needack
            pushMessage.NeedAck = buffer.ReadBoolean();
            //body
            var bodyLength = buffer.ReadInt();
            byte[] body = new byte[bodyLength];
            buffer.ReadBytes(body);
            pushMessage.Body = body;
            //create time
            pushMessage.CreatedTime = new DateTime(buffer.ReadLong());

            //extra
            var extraLength = buffer.ReadInt();
            byte[] extraBytes = new byte[extraLength];
            buffer.ReadBytes(extraBytes);
            pushMessage.Extra = extraBytes;
            output.Add(pushMessage);
        }

        /// <summary>Decode PushResponseMessage <see cref="FluentSocket.Codecs.PushResponseMessage" />
        /// </summary>
        static void DecodePushResponseMessage(MessageType messageType, IByteBuffer buffer, List<object> output)
        {
            var responseMessage = new ResponseMessage()
            {
                MessageType = MessageType.Response
            };

            //Id
            var idLength = buffer.ReadInt();
            byte[] idBytes = new byte[idLength];
            buffer.ReadBytes(idBytes);
            responseMessage.Id = DecodeStringInUtf8(idBytes);
            //code
            responseMessage.ResponseCode = buffer.ReadShort();
            //body
            var bodyLength = buffer.ReadInt();
            byte[] body = new byte[bodyLength];
            buffer.ReadBytes(body);
            responseMessage.Body = body;
            //create time
            responseMessage.CreatedTime = new DateTime(buffer.ReadLong());
            //extra
            var extraLength = buffer.ReadInt();
            byte[] extraBytes = new byte[extraLength];
            buffer.ReadBytes(extraBytes);
            responseMessage.Extra = extraBytes;

            //response.requestId
            var requestIdLength = buffer.ReadInt();
            byte[] requestIdBytes = new byte[requestIdLength];
            buffer.ReadBytes(requestIdBytes);
            responseMessage.RequestId = DecodeStringInUtf8(requestIdBytes);
            //response.requestCode
            responseMessage.RequestCode = buffer.ReadShort();
            //response.requestTime
            responseMessage.RequestTime = new DateTime(buffer.ReadLong());

            output.Add(responseMessage);
        }
        static string DecodeStringInUtf8(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
