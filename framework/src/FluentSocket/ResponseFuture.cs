using FluentSocket.Codecs;
using System;
using System.Threading.Tasks;

namespace FluentSocket
{
    public class ResponseFuture
    {
        private TaskCompletionSource<ResponseMessage> _taskSource;
        public DateTime BeginTime { get; private set; }
        public long TimeoutMillis { get; private set; }
        public RequestMessage Request { get; private set; }

        public ResponseFuture(RequestMessage request, long timeoutMillis, TaskCompletionSource<ResponseMessage> taskSource)
        {
            Request = request;
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
