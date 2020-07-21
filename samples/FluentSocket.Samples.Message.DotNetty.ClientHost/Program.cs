using FluentSocket.DotNetty;
using FluentSocket.Protocols;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Message.DotNetty.ClientHost
{
    class Program
    {
        static string _performanceKey = "SendAsync";
        static IServiceProvider _serviceProvider;
        static ILogger _logger;
        static IPerformanceService _performanceService;
        static ISocketClient _client;
        static CancellationTokenSource _cts;
        static int _sendCount = 0;
        static void Main(string[] args)
        {
            _cts = new CancellationTokenSource();
            InitializeFluentSocket();
            ClientRun();
            Console.ReadLine();
        }

        public static async void ClientRun()
        {
            _performanceService.Start();
            await _client.ConnectAsync();
            StartSendMessageTest();
        }

        static void StartSendMessageTest()
        {
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(2000);
                var serializer = _serviceProvider.GetService<IBinarySerializer>();

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_sendCount < 100000)
                    {
                        try
                        {
                            var message = new TimeRequestMessage()
                            {
                                CreateTime = DateTime.Now,
                                Content = Encoding.UTF8.GetBytes("Hello world!")
                            };
                            var responseMessage = await _client.SendMessageAsync(new RequestMessage()
                            {
                                Code = 100,
                                Body = serializer.Serialize(message)
                            });
                            Interlocked.Increment(ref _sendCount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Send message has exception:{0}", ex.Message);
                        }
                    }
                }

            }, TaskCreationOptions.LongRunning);
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
                .AddFluentSocketDotNetty();
            _serviceProvider = services.BuildServiceProvider();
            var socketFactory = _serviceProvider.GetService<IFluentSocketFactory>();

            //客户端
            var setting = new ClientSetting()
            {
                EnableHeartbeat = false,
                EnableReConnect = false,
                ReConnectDelaySeconds = 3,
                ServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21000),
                //GroupEventLoopCount = 2
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
        }
    }
}
