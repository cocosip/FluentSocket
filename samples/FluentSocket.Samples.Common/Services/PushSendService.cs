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
    public class PushSendService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly PushSendOption _option;
        private readonly IFluentSocketFactory _fluentSocketFactory;
        private readonly IPerformanceService _performanceService;
        private readonly IBinarySerializer _binarySerializer;
        private ISocketServer _server = null;

        private readonly CancellationTokenSource _cts;
        private int _sendCount = 0;
        private bool _isRunning = false;
        private bool _isClientConnected = false;
        private ISocketSession _socketSession = null;
        string _performanceKey = "PushAsync";

        public PushSendService(ILogger<PushSendService> logger, IServiceProvider serviceProvider, IOptions<PushSendOption> options, IFluentSocketFactory fluentSocketFactory, IPerformanceService performanceService, IBinarySerializer binarySerializer)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
                _logger.LogInformation("PushSendService is running!");
                return;
            }
            Initialize();
            _performanceService.Start();

            await _server.RunAsync();

            _isRunning = true;
            for (int i = 0; i < _option.PushThread; i++)
            {
                PushTask();
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
            _server = _fluentSocketFactory.CreateServer(_option.Setting);

            var sessionService = _serviceProvider.CreateInstance<CustomSessionService>(new Action<ISocketSession>(session =>
            {
                _socketSession = session;
                _isClientConnected = true;
            }));

            _server.RegisterSessionService(sessionService);

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


        private void PushTask()
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

                    if (!_isClientConnected)
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
                        var responsePush = await _server.PushAsync(new RequestPush()
                        {
                            Code = 101,
                            Body = _binarySerializer.Serialize(message)
                        }, _socketSession.SessionId);

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
