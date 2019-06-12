using FluentSocket.Codecs;
using FluentSocket.TestCommon;
using FluentSocket.TestCommon.Log4Net;
using FluentSocket.TestCommon.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.TestPushServer
{
    class Program
    {
        static int _messageCount;
        static string _performanceKey = "ReceivePushMessage";
        static string _mode;
        static SocketServer _server;
        static ILogger _logger;
        static IPerformanceService _performanceService;
        static bool _isStartedPush = false;

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
            StartPushMessageTest();
        }

        static void StartPushMessageTest()
        {

            var index = 0;
            var sendBytes = new byte[1024];

            Task.Factory.StartNew(() =>
            {
                while (_isStartedPush == false)
                {
                    Thread.Sleep(1000);
                }
                while (index < _messageCount)
                {
                    try
                    {
                        var pushMessage = new PushMessage(110, sendBytes, true);

                        _server.PushMessageToSingleClientAsync(pushMessage, x => true, 10000).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogError(t.Exception, t.Exception.Message);
                                return;
                            }
                            var pushMessageResponse = t.Result;
                            _performanceService.IncrementKeyCount(_mode, (DateTime.Now - pushMessage.CreatedTime).TotalMilliseconds);
                        });

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Send remotingRequest failed, errorMsg:{0}", ex.Message);
                    }

                    Interlocked.Increment(ref index);
                }
            });
            Console.WriteLine("开始推送了");
        }

        static void SetStartPush()
        {
            _isStartedPush = true;
        }


        static void InitializeFluentSocket()
        {
            IServiceCollection services = new ServiceCollection();
            services
                .AddLogging(f => f.AddLog4Net())
                .AddPerformance()
                .AddFluentSocket();
            var provider = services.BuildServiceProvider();
            _logger = provider.GetService<ILogger<SocketServer>>();
            var socketFactory = provider.GetService<IFluentSocketFactory>();
            
            //服务器端
            var setting = new ServerSetting()
            {
                WriteBufferLowWaterMark = 1024 * 1024 * 4,
                WriteBufferHighWaterMark = 1024 * 1024 * 2,
                SoRcvbuf = 1024 * 1024 * 2,
                SoSndbuf = 1024 * 1024 * 2,
                IsSsl = false,
                UseLibuv = false,
                EnableHeartbeat = false,
                ListeningEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22000),
                PipelineConfigure = c =>
                {
                    c.AddLast(provider.CreateInstance<ClientActiveHandler>(new Action(SetStartPush)));
                }
            };
            _server = socketFactory.CreateServer(setting);


            _mode = "Async";
            _messageCount = 1000000;
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
        }
    }


}
