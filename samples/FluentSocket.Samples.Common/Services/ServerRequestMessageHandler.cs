using FluentSocket.Protocols;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using FluentSocket.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Common.Services
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
            var timeRequestMessage = _binarySerializer.Deserialize<TimeRequestMessage>(request.Body);

            var timeResponseMessage = new TimeResponseMessage()
            {
                CreateTime = timeRequestMessage.CreateTime,
                HandleTime = DateTime.Now,
                Content = Encoding.UTF8.GetBytes($"Hello client!")
            };

            var responseMessage = new ResponseMessage()
            {
                Code = request.Code,
                Body = _binarySerializer.Serialize(timeResponseMessage)
            };

            _performanceService.IncrementKeyCount("HandleAsync", (DateTime.Now - timeRequestMessage.CreateTime).TotalMilliseconds);
            return new ValueTask<ResponseMessage>(responseMessage);
        }
    }
}
