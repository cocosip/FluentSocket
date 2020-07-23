using System;

namespace FluentSocket.Samples.Common
{
    [Serializable]
    public class TimeRequestMessage
    {
        public DateTime CreateTime { get; set; }

        public byte[] Content { get; set; }
    }
}
