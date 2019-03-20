using FluentSocket.Codecs;
using System;
using System.Threading.Tasks;

namespace FluentSocket
{
    public class PushResponseFuture
    {
        private TaskCompletionSource<PushResponseMessage> _taskSource;
        public DateTime BeginTime { get; private set; }
        public long TimeoutMillis { get; private set; }
        public PushMessage PushMessage { get; private set; }

        public PushResponseFuture(PushMessage pushMessage, long timeoutMillis, TaskCompletionSource<PushResponseMessage> taskSource)
        {
            PushMessage = pushMessage;
            TimeoutMillis = timeoutMillis;
            _taskSource = taskSource;
            BeginTime = DateTime.Now;
        }

        public bool IsTimeout()
        {
            return (DateTime.Now - BeginTime).TotalMilliseconds > TimeoutMillis;
        }
        public bool SetResponse(PushResponseMessage response)
        {
            return _taskSource.TrySetResult(response);
        }
    }
}
