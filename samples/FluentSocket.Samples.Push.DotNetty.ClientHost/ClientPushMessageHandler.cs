using FluentSocket.Protocols;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using FluentSocket.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Push.DotNetty.ClientHost
{
    public class ClientPushMessageHandler : IPushMessageHandler
    {
        private readonly IBinarySerializer _binarySerializer;
        private readonly IPerformanceService _performanceService;

        public ClientPushMessageHandler(IBinarySerializer binarySerializer, IPerformanceService performanceService)
        {
            _binarySerializer = binarySerializer;
            _performanceService = performanceService;
        }

        public ValueTask<ResponsePush> HandlePushAsync(RequestPush request)
        {
            var timeRequestMessage = _binarySerializer.Deserialize<TimeRequestMessage>(request.Body);

            var timeResponseMessage = new TimeResponseMessage()
            {
                CreateTime = timeRequestMessage.CreateTime,
                HandleTime = DateTime.Now,
                Content = Encoding.UTF8.GetBytes($"Hello Server!")
            };

            var responsePush = new ResponsePush()
            {
                Code = request.Code,
                Body = _binarySerializer.Serialize(timeResponseMessage)
            };

            _performanceService.IncrementKeyCount("Async", (DateTime.Now - timeRequestMessage.CreateTime).TotalMilliseconds);
            return new ValueTask<ResponsePush>(responsePush);
        }
    }
}
