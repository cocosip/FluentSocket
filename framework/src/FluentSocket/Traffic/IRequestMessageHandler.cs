using FluentSocket.Protocols;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public interface IRequestMessageHandler
    {
        ValueTask<ResponseMessage> HandleRequestAsync(ISocketSession session, RequestMessage request);
    }
}
