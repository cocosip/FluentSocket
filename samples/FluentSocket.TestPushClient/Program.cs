using FluentSocket.TestCommon;
using FluentSocket.TestCommon.Log4Net;
using FluentSocket.TestCommon.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FluentSocket.TestPushClient
{
    class Program
    {
        static string _performanceKey = "PushMessage";
        static string _mode;
        static SocketClient _client;
        static ILogger _logger;
        static IPerformanceService _performanceService;
        static void Main(string[] args)
        {
            //初始化框架
            InitializeFluentSocket();
            ClientRun();
            Console.ReadLine();
        }
        public static async void ClientRun()
        {
            _performanceService.Start();
            await _client.RunAsync();
            // var r = await _client.InvokeAsync(new RemotingRequestMessage(100, new byte[1]), 5000);
        }

        static void InitializeFluentSocket()
        {
            IServiceCollection services = new ServiceCollection();
            services
                .AddLogging(f => f.AddLog4Net())
                .AddPerformance()
                .AddFluentSocket();
            var provider = services.BuildServiceProvider();
            var socketFactory = provider.GetService<IFluentSocketFactory>();
            _mode = "Async";

            //客户端
            var setting = new ClientSetting()
            {
                WriteBufferLowWaterMark = 1024 * 1024 * 4,
                WriteBufferHighWaterMark = 1024 * 1024 * 2,
                SoRcvbuf = 1024 * 1024 * 2,
                SoSndbuf = 1024 * 1024 * 2,
                IsSsl = false,
                UseLibuv = false,
                EnableHeartbeat = false,
                ServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22000),
            };
            _client = socketFactory.CreateClient(setting);


            _logger = provider.GetService<ILogger<SocketClient>>();
            _performanceService = provider.GetService<IPerformanceService>();
            var performanceServiceSetting = new PerformanceServiceSetting
            {
                AutoLogging = false,
                StatIntervalSeconds = 1,
                PerformanceInfoHandler = x =>
                {
                    _logger.LogInformation("{0}, {1}, totalCount: {2}, throughput: {3}, averageThrughput: {4}, rt: {5:F3}ms, averageRT: {6:F3}ms", _performanceService.Name, _mode, x.TotalCount, x.Throughput, x.AverageThroughput, x.RT, x.AverageRT);
                }
            };
            _performanceService.Initialize(_performanceKey, performanceServiceSetting);
            _client.RegisterPushMessageHandler(110, provider.CreateInstance<ClientPushMessageHandler>(_performanceService));
        }
    }
}
