using FluentSocket.Protocols;
using System;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public class ResponseFuture
    {
        private readonly TaskCompletionSource<ResponseMessage> _taskSource;

        public short Code { get; set; }
        public DateTime BeginTime { get; private set; }
        public long TimeoutMillis { get; private set; }

        public ResponseFuture(short code, long timeoutMillis, TaskCompletionSource<ResponseMessage> taskSource)
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
        public bool SetResponse(ResponseMessage response)
        {
            return _taskSource.TrySetResult(response);
        }
    }
}
