using FluentSocket.Codecs;
using FluentSocket.TestCommon.Performance;
using FluentSocket.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluentSocket.Net45TestServer
{
    public class SeverRequestMessageHandler : IRequestMessageHandler
    {
        private readonly IPerformanceService _performanceService;

        public SeverRequestMessageHandler(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        public Task<ResponseMessage> HandleRequestAsync(RequestMessage request)
        {
            var response = new ResponseMessage(101, Encoding.UTF8.GetBytes("Hello," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")), request.Id, request.Code, request.CreatedTime);
            //_logger.LogInformation("RequestId:{0},TimeSpan:{1}",request.Id, (DateTime.Now - request.CreatedTime).TotalMilliseconds);
            _performanceService.IncrementKeyCount("Async", (DateTime.Now - request.CreatedTime).TotalMilliseconds);
            //Thread.Sleep(100);
            return Task.FromResult(response);
        }

    }
}
