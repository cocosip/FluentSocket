using FluentSocket.Codecs;
using FluentSocket.Protocols;
using FluentSocket.TestCommon;
using FluentSocket.TestCommon.Log4Net;
using FluentSocket.TestCommon.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.Net45TestClient
{
    class Program
    {
        static string _performanceKey = "SendMessage";
        static int _messageCount;
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
            StartSendMessageTest();
        }

        static void StartSendMessageTest()
        {
            var index = 0;
            var sendBytes = new byte[1024];
            //Encoding.UTF8.GetBytes("Hello,I'm client!");

            for (var i = 0; i < 3; i++)
            {
                Task.Factory.StartNew(() =>
                {
                    while (index < _messageCount)
                    {
                        try
                        {
                            var request = new RequestMessage(100, sendBytes);
                            _client.SendAsync(request, 10000).ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    _logger.LogError(t.Exception.Message);
                                    return;
                                }
                                var response = t.Result;
                                //_logger.LogInformation("接收服务器端返回:{0}" + Encoding.UTF8.GetString(response.Body));
                                if (response.ResponseCode != (int)ResponseCodes.HasException)
                                {
                                    _performanceService.IncrementKeyCount(_mode, (DateTime.Now - response.RequestTime).TotalMilliseconds);
                                }
                                else
                                {
                                    _logger.LogInformation("ResponseException,ResponseCode:{0}", response.ResponseCode);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Send remotingRequest failed, errorMsg:{0}", ex.Message);
                        }

                        Interlocked.Increment(ref index);
                    }
                });

            }


        }


        static void InitializeFluentSocket()
        {
            IServiceCollection services = new ServiceCollection();
            services
                .AddLogging()
                .AddPerformance()
                .AddFluentSocket();
            var provider = services.BuildServiceProvider();
            ILoggerFactory loggerFactory = provider.GetService<ILoggerFactory>();
            loggerFactory.AddLog4Net();

            //发送总条数
            _messageCount = 1000000;
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
                EnableReConnect = true,
                ReConnectDelaySeconds = 3,
                ServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21000),
                //GroupEventLoopCount = 2
            };

            _client = provider.CreateClient(setting);

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
        }



    }
}
