using FluentSocket.Protocols;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public interface IPushMessageHandler
    {
        ValueTask<ResponsePush> HandlePushAsync(RequestPush request);
    }
}
