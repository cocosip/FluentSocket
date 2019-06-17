using FluentSocket.Codecs;
using FluentSocket.TestCommon.Performance;
using FluentSocket.Traffic;
using System;
using System.Text;

namespace FluentSocket.TestPushClient
{
    public class ClientPushMessageHandler : BasePushMessageHandler
    {
        private readonly IPerformanceService _performanceService;

        public ClientPushMessageHandler(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        public override PushResponseMessage HandlePushMessage(PushMessage pushMessage)
        {
            _performanceService.IncrementKeyCount("Async", (DateTime.Now - pushMessage.CreatedTime).TotalMilliseconds);
            var pushResponseMessage = new PushResponseMessage(105, Encoding.UTF8.GetBytes("hello"), pushMessage.Id, pushMessage.Code, pushMessage.CreatedTime);
            return pushResponseMessage;
        }
    }
}
