using FluentSocket.DotNetty;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FluentSocket.Samples.Push.DotNetty.ClientHost
{
    class Program
    {
        static string _performanceKey = "SendAsync";
        static ILogger _logger;
        static IServiceProvider _serviceProvider;
        static ISocketClient _client;
        static IPerformanceService _performanceService;

        static void Main(string[] args)
        {
            InitializeFluentSocket();
            ClientRun();
            Console.ReadLine();
        }

        public static async void ClientRun()
        {
            _performanceService.Start();
            await _client.ConnectAsync();
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
                .AddSamples()
                .AddFluentSocket()
                .AddFluentSocketDotNetty();
            _serviceProvider = services.BuildServiceProvider();
            var socketFactory = _serviceProvider.GetService<IFluentSocketFactory>();

            //客户端
            var setting = new ClientSetting()
            {
                EnableHeartbeat = true,
                EnableReConnect = true,
                ReConnectMaxCount = 1000,
                ReConnectDelaySeconds = 1,
                ServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22000),
                PushReqCapacity = 20000,
            };

            _client = socketFactory.CreateClient(setting);

            _logger = _serviceProvider.GetService<ILogger<ISocketClient>>();
            _performanceService = _serviceProvider.GetService<IPerformanceService>();
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

            var handler = _serviceProvider.CreateInstance<ClientPushMessageHandler>();
            _client.RegisterPushHandler(101, handler);

        }
    }
}
