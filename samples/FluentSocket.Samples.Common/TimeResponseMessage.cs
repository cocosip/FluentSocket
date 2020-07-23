using System;

namespace FluentSocket.Samples.Common
{
    [Serializable]
    public class TimeResponseMessage
    {
        public DateTime CreateTime { get; set; }

        public DateTime HandleTime { get; set; }

        public byte[] Content { get; set; }
    }
}
