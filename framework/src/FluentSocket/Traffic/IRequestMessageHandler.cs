using FluentSocket.Codecs;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public interface IRequestMessageHandler
    {
        Task<ResponseMessage> HandleRequestAsync(RequestMessage request);
    }

}
