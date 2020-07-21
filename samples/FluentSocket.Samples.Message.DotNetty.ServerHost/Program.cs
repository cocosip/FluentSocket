using FluentSocket.DotNetty;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FluentSocket.Samples.Message.DotNetty.ServerHost
{
    class Program
    {
        static string _performanceKey = "Async";
        static ILogger _logger;
        static IPerformanceService _performanceService;
        static ISocketServer _socketServer;
        static void Main(string[] args)
        {
            InitializeFluentSocket();
            ServerRun();
            Console.ReadLine();
        }

        public static async void ServerRun()
        {
            _performanceService.Start();
            await _socketServer.RunAsync();
        }

        static void InitializeFluentSocket()
        {
            IServiceCollection services = new ServiceCollection();
            services
                .AddLogging(l =>
                {
                    l.SetMinimumLevel(LogLevel.Debug);
                    l.AddConsole();
                })
                .AddSerialize()
                .AddPerformance()
                .AddFluentSocket()
                .AddFluentSocketDotNetty()
                ;
            var serviceProvider = services.BuildServiceProvider();
            var socketFactory = serviceProvider.GetService<IFluentSocketFactory>();


            //客户端
            var setting = new ServerSetting()
            {
                ListeningEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21000),
                HandleMessageReqThread = 1
            };
            _socketServer = socketFactory.CreateServer(setting);

            _logger = serviceProvider.GetService<ILogger<ISocketServer>>();
            _performanceService = serviceProvider.GetService<IPerformanceService>();
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

            var handler = serviceProvider.CreateInstance<ServerRequestMessageHandler>();
            _socketServer.RegisterRequestHandler(100, handler);
        }
    }
}
