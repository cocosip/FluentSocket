using FluentSocket.DotNetty;
using FluentSocket.Protocols;
using FluentSocket.Samples.Common;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Push.DotNetty.ServerHost
{
    class Program
    {
        static string _performanceKey = "Async";
        static ILogger _logger;
        static IPerformanceService _performanceService;
        static IBinarySerializer _binarySerializer;
        static ISocketServer _socketServer;
        static bool _clientConnected = false;
        static ISocketSession _socketSession = null;
        static int _pushCount = 0;
        static CancellationTokenSource _cts = new CancellationTokenSource();
        static void Main(string[] args)
        {
            InitializeFluentSocket();
            ServerRun();
            Push();
            Console.ReadLine();
        }

        public static async void ServerRun()
        {
            _performanceService.Start();
            await _socketServer.RunAsync();
        }

        public static void Push()
        {
            Task.Factory.StartNew(async () =>
            {
                var content = new byte[1024];

                while (true)
                {
                    if (!_clientConnected)
                    {
                        await Task.Delay(2000);
                        continue;
                    }
                    try
                    {
                        if (_pushCount < 1000000)
                        {
                            var timeRequestMessage = new TimeRequestMessage()
                            {
                                CreateTime = DateTime.Now,
                                Content = content
                            };

                            var requestPush = new RequestPush()
                            {
                                Code = 101,
                                Body = _binarySerializer.Serialize(timeRequestMessage)
                            };

                            var pushResponse = await _socketServer.PushAsync(requestPush, _socketSession);
                            if (pushResponse.Body != null)
                            {

                                var timeResponseMessage = _binarySerializer.Deserialize<TimeResponseMessage>(pushResponse.Body);

                                _performanceService.IncrementKeyCount("Async", (DateTime.Now - timeResponseMessage.CreateTime).TotalMilliseconds);
                                Interlocked.Increment(ref _pushCount);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Push message caught exception,{0}", ex.Message);
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
                .AddSamples()
                .AddFluentSocket()
                .AddFluentSocketDotNetty()
                ;
            var serviceProvider = services.BuildServiceProvider();
            var socketFactory = serviceProvider.GetService<IFluentSocketFactory>();

            var setting = new ServerSetting()
            {
                ListeningEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22000),
                HandleMessageReqThread = 1,
                MessageReqCapacity = 10000,
            };
            _socketServer = socketFactory.CreateServer(setting);

            _logger = serviceProvider.GetService<ILogger<ISocketServer>>();
            _binarySerializer = serviceProvider.GetService<IBinarySerializer>();
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

            var sessionService = serviceProvider.CreateInstance<CustomSessionService>(new Action<ISocketSession>(session =>
            {
                _socketSession = session;
                _clientConnected = true;
            }));

            _socketServer.RegisterSessionService(sessionService);

        }
    }
}
