using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using FluentSocket.Codecs;
using FluentSocket.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FluentSocket.Tests.Codecs
{
    public class MessageCodecTests
    {
        static readonly IByteBufferAllocator Allocator = new UnpooledByteBufferAllocator();
        readonly MessageDecoder serverDecoder;
        readonly MessageDecoder clientDecoder;
        readonly Mock<IChannelHandlerContext> contextMock;

        public MessageCodecTests()
        {
            this.serverDecoder = new MessageDecoder();
            this.clientDecoder = new MessageDecoder();
            this.contextMock = new Mock<IChannelHandlerContext>(MockBehavior.Strict);
            this.contextMock.Setup(x => x.Removed).Returns(false);
            this.contextMock.Setup(x => x.Allocator).Returns(UnpooledByteBufferAllocator.Default);
        }

        [Theory]
        [InlineData("123456", 101, "ssss")]
        [InlineData("10000", 121, "helloworld")]
        [InlineData("333333", 0, "hahaha")]
        public void TestRequestMessage(string id, short code, string bodyContent)
        {
            var createdTime = Convert.ToDateTime("2018-01-01 12:00:00");
            var requestMessage = new RequestMessage()
            {
                Id = id,
                Code = code,
                Body = Encoding.UTF8.GetBytes(bodyContent),
                MessageType = MessageType.Request,
                CreatedTime = createdTime,
                Extra = ByteUtil.EncodeStringInUtf8("hello")
            };

            RequestMessage recoded = this.RecodeMessage(requestMessage, true, true);
            this.contextMock.Verify(x => x.FireChannelRead(It.IsAny<RequestMessage>()), Times.Once);
            Assert.Equal(requestMessage.Id, recoded.Id);
            Assert.Equal(requestMessage.Code, recoded.Code);
            Assert.Equal(requestMessage.Body, recoded.Body);
            Assert.Equal(requestMessage.CreatedTime, recoded.CreatedTime);
            Assert.Equal(requestMessage.Extra, recoded.Extra);
        }

        [Theory]
        [InlineData("333122", -3, "··！", true)]
        [InlineData("1233d", 0, "helloworld", false)]
        [InlineData("vqqw3", 222, "hahaha", true)]
        public void TestPushMessage(string id, short code, string bodyContent, bool needAck)
        {
            var createdTime = Convert.ToDateTime("2018-01-01 12:00:00");
            var pushMessage = new PushMessage()
            {
                Id = id,
                Code = code,
                NeedAck = needAck,
                Body = Encoding.UTF8.GetBytes(bodyContent),
                MessageType = MessageType.PushRequest,
                CreatedTime = createdTime,
                Extra = ByteUtil.EncodeStringInUtf8("hello")
            };

            PushMessage recoded = this.RecodeMessage(pushMessage, true, true);
            this.contextMock.Verify(x => x.FireChannelRead(It.IsAny<PushMessage>()), Times.Once);
            Assert.Equal(pushMessage.Id, recoded.Id);
            Assert.Equal(pushMessage.NeedAck, recoded.NeedAck);
            Assert.Equal(pushMessage.Code, recoded.Code);
            Assert.Equal(pushMessage.Body, recoded.Body);
            Assert.Equal(pushMessage.CreatedTime, recoded.CreatedTime);
            Assert.Equal(pushMessage.Extra, recoded.Extra);
        }


        [Theory]
        [InlineData("x233q`", 2, "··！", "55212", 333)]
        [InlineData("qqq", -1, "weqw1wq2", "Je`qdsw2", -100)]
        [InlineData("  ", 1222, "aweowwwq", "55212", 22)]
        [InlineData("123qweasd", 323, "---zzzz----", "55212", -3)]
        public void TestResponseMessage(string id, short code, string bodyContent, string requestId, short requestCode)
        {
            var createdTime = Convert.ToDateTime("2018-01-01 12:00:00");
            var requestCreatedTime = Convert.ToDateTime("2018-12-31 12:00:00");
            var responseMessage = new ResponseMessage()
            {
                Id = id,
                ResponseCode = code,
                Body = Encoding.UTF8.GetBytes(bodyContent),
                MessageType = MessageType.Response,
                CreatedTime = createdTime,
                Extra = ByteUtil.EncodeStringInUtf8("haha"),
                RequestId = requestId,
                RequestCode = requestCode,
                RequestTime = requestCreatedTime
            };

            ResponseMessage recoded = this.RecodeMessage(responseMessage, true, true);
            this.contextMock.Verify(x => x.FireChannelRead(It.IsAny<ResponseMessage>()), Times.Once);
            Assert.Equal(responseMessage.Id, recoded.Id);
            Assert.Equal(responseMessage.ResponseCode, recoded.ResponseCode);
            Assert.Equal(responseMessage.Body, recoded.Body);
            Assert.Equal(responseMessage.CreatedTime, recoded.CreatedTime);
            Assert.Equal(responseMessage.Extra, recoded.Extra);
            Assert.Equal(responseMessage.RequestId, recoded.RequestId);
            Assert.Equal(responseMessage.RequestCode, recoded.RequestCode);
            Assert.Equal(responseMessage.RequestTime, recoded.RequestTime);
        }


        T RecodeMessage<T>(T message, bool useServer, bool explodeForDecode)
         where T : Message
        {
            var output = new List<object>();
            MessageEncoder.DoEncode(Allocator, message, output);

            T observedPacket = null;
            this.contextMock.Setup(x => x.FireChannelRead(It.IsAny<T>()))
                .Callback((object m) => observedPacket = Assert.IsAssignableFrom<T>(m))
                .Returns(this.contextMock.Object);

            foreach (IByteBuffer byteBuffer in output)
            {
                MessageDecoder remotingMessageDecoder = useServer ? this.serverDecoder : this.clientDecoder;
                if (explodeForDecode)
                {
                    while (byteBuffer.IsReadable())
                    {
                        //IByteBuffer finalBuffer = message.ReadBytes(1);
                        remotingMessageDecoder.ChannelRead(this.contextMock.Object, byteBuffer);
                    }
                }
                else
                {
                    remotingMessageDecoder.ChannelRead(this.contextMock.Object, byteBuffer);
                }
            }
            return observedPacket;
        }

    }
}
