using FluentSocket.Codecs;
using FluentSocket.TestCommon.Performance;
using FluentSocket.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluentSocket.TestPushClient
{
    public class ClientPushMessageHandler : IPushMessageHandler
    {
        private readonly IPerformanceService _performanceService;

        public ClientPushMessageHandler(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        public Task<PushResponseMessage> HandlePushMessageAsync(PushMessage pushMessage)
        {
            _performanceService.IncrementKeyCount("Async", (DateTime.Now - pushMessage.CreatedTime).TotalMilliseconds);
            var pushResponseMessage = new PushResponseMessage(105, Encoding.UTF8.GetBytes("hello"), pushMessage.Id, pushMessage.Code, pushMessage.CreatedTime);
            return Task.FromResult(pushResponseMessage);
        }

 
    }
}
