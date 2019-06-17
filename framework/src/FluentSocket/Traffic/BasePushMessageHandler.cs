using FluentSocket.Codecs;
using System;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public abstract class BasePushMessageHandler : IPushMessageHandler
    {
        public virtual PushResponseMessage HandlePushMessage(PushMessage pushMessage)
        {
            throw new NotImplementedException();
        }

        public virtual Task<PushResponseMessage> HandlePushMessageAsync(PushMessage pushMessage)
        {
            throw new NotImplementedException();
        }
    }
}
