using FluentSocket.Protocols;
using System;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public class PushFuture
    {
        private readonly TaskCompletionSource<ResponsePush> _taskSource;

        public short Code { get; set; }
        public DateTime BeginTime { get; private set; }
        public long TimeoutMillis { get; private set; }

        public PushFuture(short code, long timeoutMillis, TaskCompletionSource<ResponsePush> taskSource)
        {
            Code = code;
            TimeoutMillis = timeoutMillis;
            _taskSource = taskSource;
            BeginTime = DateTime.Now;
        }
        public bool IsTimeout()
        {
            return (DateTime.Now - BeginTime).TotalMilliseconds > TimeoutMillis;
        }
        public bool SetResponse(ResponsePush response)
        {
            return _taskSource.TrySetResult(response);
        }
    }
}
