﻿using FluentSocket.Codecs;
using FluentSocket.TestCommon.Performance;
using FluentSocket.Traffic;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.TestServer
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
            return Task.Run<ResponseMessage>(() =>
            {
                var response = new ResponseMessage(101, Encoding.UTF8.GetBytes("Hello," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")), request.Id, request.Code, request.CreatedTime);
                //_logger.LogInformation("RequestId:{0},TimeSpan:{1}",request.Id, (DateTime.Now - request.CreatedTime).TotalMilliseconds);
                _performanceService.IncrementKeyCount("Async", (DateTime.Now - request.CreatedTime).TotalMilliseconds);
                Thread.Sleep(10);
                return response;
            });

        }
    }
}
