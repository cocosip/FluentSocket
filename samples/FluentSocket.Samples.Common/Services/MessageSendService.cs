using FluentSocket.Protocols;
using FluentSocket.Samples.Common.Performance;
using FluentSocket.Samples.Common.Serializing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Common.Services
{
    public class MessageSendService
    {
        private readonly ILogger _logger;
        private readonly MessageSendOption _option;
        private readonly IFluentSocketFactory _fluentSocketFactory;
        private readonly IPerformanceService _performanceService;
        private readonly IBinarySerializer _binarySerializer;
        private ISocketClient _client = null;

        private readonly CancellationTokenSource _cts;
        private int _sendCount = 0;
        private bool _isRunning = false;
        static string _performanceKey = "SendAsync";

        public MessageSendService(ILogger<MessageSendService> logger, IOptions<MessageSendOption> options, IFluentSocketFactory fluentSocketFactory, IPerformanceService performanceService, IBinarySerializer binarySerializer)
        {
            _logger = logger;
            _option = options.Value;
            _fluentSocketFactory = fluentSocketFactory;
            _performanceService = performanceService;
            _binarySerializer = binarySerializer;

            _cts = new CancellationTokenSource();
        }

        public async ValueTask Start()
        {
            if (_isRunning)
            {
                _logger.LogInformation("MessageSendService is running!");
                return;
            }
            Initialize();
            _performanceService.Start();

            await _client.ConnectAsync();

            _isRunning = true;
            for (int i = 0; i < _option.SendThread; i++)
            {
                SendMessageTask();
            }
        }


        public ValueTask Shutdown()
        {
            _isRunning = false;
            _cts.Cancel();
            return new ValueTask();
        }

        private void Initialize()
        {
            _client = _fluentSocketFactory.CreateClient(_option.Setting);
            var performanceServiceSetting = new PerformanceServiceSetting
            {
                AutoLogging = false,
                StatIntervalSeconds = 1,
                PerformanceInfoHandler = x =>
                {
                    _logger.LogInformation("{0}, {1}, totalCount: {2}, throughput: {3}, averageThrughput: {4}, rt: {5:F3}ms, averageRT: {6:F3}ms", _performanceService.Name, _performanceKey, x.TotalCount, x.Throughput, x.AverageThroughput, x.RT, x.AverageRT);
                }
            };
            _performanceService.Initialize(_performanceKey, performanceServiceSetting);
        }

        private void SendMessageTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_sendCount >= _option.Total)
                    {
                        await Task.Delay(2000);
                        continue;
                    }

                    try
                    {
                        var message = new TimeRequestMessage()
                        {
                            CreateTime = DateTime.Now,
                            Content = Encoding.UTF8.GetBytes($"{DateTime.Now:yyyy-MM-dd HH:mm:ss fff}")
                        };
                        var responseMessage = await _client.SendMessageAsync(new RequestMessage()
                        {
                            Code = 100,
                            Body = _binarySerializer.Serialize(message)
                        });

                        _performanceService.IncrementKeyCount(_performanceKey, (DateTime.Now - message.CreateTime).TotalMilliseconds);
                        Interlocked.Increment(ref _sendCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Send message caught exception,{0}", ex.Message);
                    }

                }
            }, TaskCreationOptions.LongRunning);
        }


    }
}
