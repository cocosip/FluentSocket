using FluentSocket.Protocols;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using FluentSocket.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Message.DotNetty.ServerHost
{
    public class ServerRequestMessageHandler : IRequestMessageHandler
    {
        private readonly IBinarySerializer _binarySerializer;
        private readonly IPerformanceService _performanceService;

        public ServerRequestMessageHandler(IPerformanceService performanceService, IBinarySerializer binarySerializer)
        {
            _performanceService = performanceService;
            _binarySerializer = binarySerializer;
        }

        public ValueTask<ResponseMessage> HandleRequestAsync(ISocketSession session, RequestMessage request)
        {
            var responseMessage = new ResponseMessage()
            {
                Code = request.Code,
                Body = Encoding.UTF8.GetBytes($"Hello client!{DateTime.Now:yyyy-MM-dd hh:mm:ss fff}")
            };

            var timeRequestMessage = _binarySerializer.Deserialize<TimeRequestMessage>(request.Body);

            _performanceService.IncrementKeyCount("Async", (DateTime.Now - timeRequestMessage.CreateTime).TotalMilliseconds);
            return new ValueTask<ResponseMessage>(responseMessage);
        }
    }
}
