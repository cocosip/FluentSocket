using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSocket.Codecs
{
    public class Message
    {
        public string Id { get; set; }
        public MessageType MessageType { get; set; }
        public byte[] Body { get; set; }
        public DateTime CreatedTime { get; set; }
        public byte[] Extra { get; set; }

        public Message()
        {

        }

        public Message(string id, MessageType messageType, byte[] body, DateTime createdTime, byte[] extra)
        {
            Id = id;
            MessageType = messageType;
            Body = body;
            CreatedTime = createdTime;
            Extra = extra;
        }
    }
}
