using DotNetty.Transport.Channels;
using FluentSocket.Traffic;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty
{
    public class DotNettySocketServer : ISocketServer
    {
        /// <summary>Listening ip address and port
        /// </summary>
        public IPEndPoint ListeningEndPoint => _setting.ListeningEndPoint;

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerSetting _setting;
        private readonly DotNettyServerSetting _extraSetting;

        private IEventLoopGroup _bossGroup = null;
        private IEventLoopGroup _workerGroup = null;
        private IChannel _boundChannel = null;

        private readonly CancellationTokenSource _cts;
        private readonly ConcurrentDictionary<short, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ConcurrentDictionary<int, ResponseFuture> _responseFutureDict;
        public DotNettySocketServer(ILogger<DotNettySocketServer> logger, IServiceProvider serviceProvider, ServerSetting setting)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            _setting = setting;
            if (!_setting.ExtraSettings.Any())
            {
                _setting.ExtraSettings.Add(new DotNettyClientSetting());
            }
            _extraSetting = (DotNettyServerSetting)_setting.ExtraSettings.FirstOrDefault();

            _cts = new CancellationTokenSource();

            _requestMessageHandlerDict = new ConcurrentDictionary<short, IRequestMessageHandler>();
            _responseFutureDict = new ConcurrentDictionary<int, ResponseFuture>();
        }

        public async ValueTask RunAsync()
        {
            if (_boundChannel != null)
            {
                _logger.LogInformation($"Server is running! Don't run again! ChannelId:{_boundChannel.Id.AsShortText()}");
                return;
            }
        }


        /// <summary>Register RequestHandler
        /// </summary>
        public void RegisterRequestMessageHandler(short code, IRequestMessageHandler handler)
        {
            if (!_requestMessageHandlerDict.TryAdd(code, handler))
            {
                _logger.LogInformation("RegisterRequestMessageHandler fail! Code {0}", code);
            }
        }

    }
}
