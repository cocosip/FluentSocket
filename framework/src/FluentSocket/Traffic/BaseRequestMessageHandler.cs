using FluentSocket.Codecs;
using System.Threading.Tasks;

namespace FluentSocket.Traffic
{
    public abstract class BaseRequestMessageHandler : IRequestMessageHandler
    {
        public virtual ResponseMessage HandleRequest(RequestMessage request)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task<ResponseMessage> HandleRequestAsync(RequestMessage request)
        {
            throw new System.NotImplementedException();
        }
    }
}
