using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using FluentSocket.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.Codecs
{

    public class MessageEncoder : MessageToMessageEncoder<Message>
    {
        public MessageEncoder()
        {

        }

        protected override void Encode(IChannelHandlerContext context, Message message, List<object> output) => DoEncode(context.Allocator, message, output);

        public static void DoEncode(IByteBufferAllocator bufferAllocator, Message message, List<object> output)
        {
            switch (message.MessageType)
            {
                case MessageType.Request:
                    EncodeRequestMessage(bufferAllocator, (RequestMessage)message, output);
                    break;
                case MessageType.Response:
                    EncodeResponseMessage(bufferAllocator, (ResponseMessage)message, output);
                    break;
                case MessageType.PushRequest:
                    EncodePushMessage(bufferAllocator, (PushMessage)message, output);
                    break;
                case MessageType.PushResponse:
                    EncodePushResponseMessage(bufferAllocator, (PushResponseMessage)message, output);
                    break;
                default:
                    throw new ArgumentException("Unknown message type: " + message.MessageType, nameof(message));
            }
        }

        /// <summary>Encode RequestMessage <see cref="FluentSocket.Codecs.RequestMessage" />
        /// </summary>
        static void EncodeRequestMessage(IByteBufferAllocator bufferAllocator, RequestMessage requestMessage, List<object> output)
        {
            IByteBuffer buffer = null;
            if (requestMessage.Body == null)
            {
                requestMessage.Body = ByteUtil.EmptyBytes;
            }
            if (requestMessage.Extra == null)
            {
                requestMessage.Extra = ByteUtil.EmptyBytes;
            }

            try
            {
                var idBytes = EncodeStringInUtf8(requestMessage.Id);

                //Id  + Body +Extra +(length:4 *3),12为3个int的长度
                var byteBufferLength = idBytes.Length + requestMessage.Extra.Length + requestMessage.Body.Length + 12;

                //MessageType(2) + Code(4) + Time(8)
                var fixedLength = 14;

                buffer = bufferAllocator.Buffer(byteBufferLength + fixedLength);
                //messageType
                buffer.WriteShort((short)requestMessage.MessageType);
                //write id
                buffer.WriteInt(idBytes.Length);
                buffer.WriteBytes(idBytes);

                //write code
                buffer.WriteInt(requestMessage.Code);

                //write body
                buffer.WriteInt(requestMessage.Body.Length);
                buffer.WriteBytes(requestMessage.Body);

                //write create time
                buffer.WriteLong(requestMessage.CreatedTime.Ticks);

                //write extra
                buffer.WriteInt(requestMessage.Extra.Length);
                buffer.WriteBytes(requestMessage.Extra);

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        /// <summary>Encode ResponseMessage <see cref="FluentSocket.Codecs.ResponseMessage" />
        /// </summary>
        static void EncodeResponseMessage(IByteBufferAllocator bufferAllocator, ResponseMessage responseMessage, List<object> output)
        {
            IByteBuffer buffer = null;
            if (responseMessage.Body == null)
            {
                responseMessage.Body = ByteUtil.EmptyBytes;
            }
            if (responseMessage.Extra == null)
            {
                responseMessage.Extra = ByteUtil.EmptyBytes;
            }

            try
            {

                var idBytes = EncodeStringInUtf8(responseMessage.Id);
                var requestIdBytes = EncodeStringInUtf8(responseMessage.RequestId);

                //Id + Body +Extra + (length: 4*3)
                var byteBufferLength = idBytes.Length + responseMessage.Extra.Length + responseMessage.Body.Length + 12;

                //MessageType(2) + Code(4) + Time(8)
                var fixedLength = 14;

                //RequestId+RequestCode(4) +RequestTime(8)
                var requestLength = requestIdBytes.Length + 4 + 4 + 8;

                buffer = bufferAllocator.Buffer(byteBufferLength + fixedLength + requestLength);

                //messageType
                buffer.WriteShort((short)responseMessage.MessageType);
                //write id
                buffer.WriteInt(idBytes.Length);
                buffer.WriteBytes(idBytes);
                //write response code
                buffer.WriteInt(responseMessage.ResponseCode);
                //write body
                buffer.WriteInt(responseMessage.Body.Length);
                buffer.WriteBytes(responseMessage.Body);
                //write create time
                buffer.WriteLong(responseMessage.CreatedTime.Ticks);
                //write extra
                buffer.WriteInt(responseMessage.Extra.Length);
                buffer.WriteBytes(responseMessage.Extra);

                //write response.requestId
                buffer.WriteInt(requestIdBytes.Length);
                buffer.WriteBytes(requestIdBytes);
                //write response.requestCode
                buffer.WriteInt(responseMessage.RequestCode);
                //write response.requestTime
                buffer.WriteLong(responseMessage.RequestTime.Ticks);


                output.Add(buffer);
                buffer = null;

            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        /// <summary>Encode PushMessage <see cref="FluentSocket.Codecs.PushMessage" />
        /// </summary>
        static void EncodePushMessage(IByteBufferAllocator bufferAllocator, PushMessage pushMessage, List<object> output)
        {
            IByteBuffer buffer = null;
            if (pushMessage.Body == null)
            {
                pushMessage.Body = ByteUtil.EmptyBytes;
            }
            if (pushMessage.Extra == null)
            {
                pushMessage.Extra = ByteUtil.EmptyBytes;
            }

            try
            {
                var idBytes = EncodeStringInUtf8(pushMessage.Id);

                //Id  + Body +Extra +(length:4 *3),12为3个int的长度
                var byteBufferLength = idBytes.Length + pushMessage.Extra.Length + pushMessage.Body.Length + 12;

                //MessageType(2) + Code(4) +NeedAck(2)+ Time(8)
                var fixedLength = 16;

                buffer = bufferAllocator.Buffer(byteBufferLength + fixedLength);
                //messageType
                buffer.WriteShort((short)pushMessage.MessageType);
                //write id
                buffer.WriteInt(idBytes.Length);
                buffer.WriteBytes(idBytes);

                //write code
                buffer.WriteInt(pushMessage.Code);
                //write needAck
                buffer.WriteBoolean(pushMessage.NeedAck);

                //write body
                buffer.WriteInt(pushMessage.Body.Length);
                buffer.WriteBytes(pushMessage.Body);

                //write create time
                buffer.WriteLong(pushMessage.CreatedTime.Ticks);

                //write extra
                buffer.WriteInt(pushMessage.Extra.Length);
                buffer.WriteBytes(pushMessage.Extra);

                output.Add(buffer);
                buffer = null;
            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        /// <summary>Encode PushResponseMessage <see cref="FluentSocket.Codecs.PushResponseMessage" />
        /// </summary>
        static void EncodePushResponseMessage(IByteBufferAllocator bufferAllocator, PushResponseMessage pushResponseMessage, List<object> output)
        {
            IByteBuffer buffer = null;
            if (pushResponseMessage.Body == null)
            {
                pushResponseMessage.Body = ByteUtil.EmptyBytes;
            }
            if (pushResponseMessage.Extra == null)
            {
                pushResponseMessage.Extra = ByteUtil.EmptyBytes;
            }

            try
            {

                var idBytes = EncodeStringInUtf8(pushResponseMessage.Id);
                var requestIdBytes = EncodeStringInUtf8(pushResponseMessage.RequestId);

                //Id + Body +Extra + (length: 4*3)
                var byteBufferLength = idBytes.Length + pushResponseMessage.Extra.Length + pushResponseMessage.Body.Length + 12;

                //MessageType(2) + Code(4) + Time(8)
                var fixedLength = 14;

                //RequestId+RequestCode(4) +RequestTime(8)
                var requestLength = requestIdBytes.Length + 4 + 4 + 8;

                buffer = bufferAllocator.Buffer(byteBufferLength + fixedLength + requestLength);

                //messageType
                buffer.WriteShort((short)pushResponseMessage.MessageType);
                //write id
                buffer.WriteInt(idBytes.Length);
                buffer.WriteBytes(idBytes);
                //write response code
                buffer.WriteInt(pushResponseMessage.ResponseCode);
                //write body
                buffer.WriteInt(pushResponseMessage.Body.Length);
                buffer.WriteBytes(pushResponseMessage.Body);
                //write create time
                buffer.WriteLong(pushResponseMessage.CreatedTime.Ticks);
                //write extra
                buffer.WriteInt(pushResponseMessage.Extra.Length);
                buffer.WriteBytes(pushResponseMessage.Extra);

                //write response.requestId
                buffer.WriteInt(requestIdBytes.Length);
                buffer.WriteBytes(requestIdBytes);
                //write response.requestCode
                buffer.WriteInt(pushResponseMessage.RequestCode);
                //write response.requestTime
                buffer.WriteLong(pushResponseMessage.RequestTime.Ticks);


                output.Add(buffer);
                buffer = null;

            }
            finally
            {
                buffer?.SafeRelease();
            }
        }

        static byte[] EncodeStringInUtf8(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
    }
}
