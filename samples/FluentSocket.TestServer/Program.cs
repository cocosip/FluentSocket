using FluentSocket.TestCommon;
using FluentSocket.TestCommon.Log4Net;
using FluentSocket.TestCommon.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FluentSocket.TestServer
{
    class Program
    {

        static string _performanceKey = "ReceiveMessage";
        static SocketServer _server;
        static ILogger _logger;
        static IPerformanceService _performanceService;

        static void Main(string[] args)
        {

            InitializeFluentSocket();
            ServerRun();
            Console.ReadLine();
        }

        public static async void ServerRun()
        {
            _performanceService.Start();
            await _server.RunAsync();
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


            //客户端
            var setting = new ServerSetting()
            {
                WriteBufferLowWaterMark = 1024 * 1024 * 4,
                WriteBufferHighWaterMark = 1024 * 1024 * 2,
                SoRcvbuf = 1024 * 1024 * 2,
                SoSndbuf = 1024 * 1024 * 2,
                IsSsl = false,
                UseLibuv = false,
                EnableHeartbeat = false,
                ListeningEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21000),
                BusinessEventLoopCount=1
                //BossGroupEventLoopCount = 1,
                //WorkGroupEventLoopCount = 2,
            };
            _server = socketFactory.CreateServer(setting);

            _logger = provider.GetService<ILogger<SocketServer>>();
            _performanceService = provider.GetService<IPerformanceService>();
            var performanceServiceSetting = new PerformanceServiceSetting
            {
                AutoLogging = false,
                StatIntervalSeconds = 1,
                PerformanceInfoHandler = x =>
                {
                    _logger.LogInformation("{0}, {1}, totalCount: {2}, throughput: {3}, averageThrughput: {4}, rt: {5:F3}ms, averageRT: {6:F3}ms", _performanceService.Name, "Async", x.TotalCount, x.Throughput, x.AverageThroughput, x.RT, x.AverageRT);
                }
            };
            _performanceService.Initialize(_performanceKey, performanceServiceSetting);

            _server.RegisterRequestMessageHandler(100, new SeverRequestMessageHandler(_performanceService));
        }

    }
}
