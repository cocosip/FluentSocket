using FluentSocket.Codecs;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public interface IPushMessageHandler
    {
        PushResponseMessage HandlePushMessage(PushMessage pushMessage);
    }
}
