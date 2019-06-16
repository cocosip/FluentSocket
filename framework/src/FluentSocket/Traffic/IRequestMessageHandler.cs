using FluentSocket.Codecs;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public interface IRequestMessageHandler
    {
        ResponseMessage HandleRequest(RequestMessage request);
    }

}
