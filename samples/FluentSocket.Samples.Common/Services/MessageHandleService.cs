using FluentSocket.Samples.Common.Performance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.Samples.Common.Services
{
    public class MessageHandleService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MessageHandleOption _option;
        private readonly IPerformanceService _performanceService;
        private readonly IFluentSocketFactory _fluentSocketFactory;

        private ISocketServer _server = null;

        private readonly CancellationTokenSource _cts;
        private bool _isRunning = false;
        string _performanceKey = "HandleAsync";
        public MessageHandleService(ILogger<MessageHandleService> logger, IServiceProvider serviceProvider, IOptions<MessageHandleOption> options, IPerformanceService performanceService, IFluentSocketFactory fluentSocketFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _option = options.Value;
            _performanceService = performanceService;
            _fluentSocketFactory = fluentSocketFactory;

            _cts = new CancellationTokenSource();
        }


        public async ValueTask Start()
        {
            if (_isRunning)
            {
                _logger.LogInformation("MessageHandleService is running!");
                return;
            }
            Initialize();
            _performanceService.Start();

            await _server.RunAsync();
            _isRunning = true;
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
            var handler = _serviceProvider.CreateInstance<ServerRequestMessageHandler>();
            _server.RegisterRequestHandler(100, handler);

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


    }
}
