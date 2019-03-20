using FluentSocket.Codecs;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public interface IPushMessageHandler
    {
        Task<PushResponseMessage> HandlePushMessageAsync(PushMessage pushMessage);
    }
}
