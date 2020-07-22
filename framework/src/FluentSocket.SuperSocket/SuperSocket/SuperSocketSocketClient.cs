using FluentSocket.Protocols;
using FluentSocket.Traffic;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SystemChannel = System.Threading.Channels.Channel;
using SystemChannels = System.Threading.Channels;

namespace FluentSocket.SuperSocket
{
    public class SuperSocketSocketClient : ISocketClient
    {
        /// <summary>ServerEndPoint
        /// </summary>
        public IPEndPoint ServerEndPoint => _setting.ServerEndPoint;

        /// <summary>LocalEndPoint
        /// </summary>
        public IPEndPoint LocalEndPoint => _setting.LocalEndPoint;

        public bool IsConnected => _isConnected;

        private readonly ILogger _logger;
        private readonly IFluentSocketApplication _app;
        private readonly ISocketSessionBuilder _socketSessionBuilder;
        private readonly ClientSetting _setting;

        private readonly SuperSocketClientSetting _extraSetting;

        private IEasyClient<Packet> _easyClient = null;

        private bool _isConnected = false;
        //private int _reConnectAttempt = 0;
        //private int _sequence = 1;



        private readonly CancellationTokenSource _cts;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ManualResetEventSlim _manualResetEventSlim;
        private readonly SystemChannels.Channel<PushReqPacket> _reqPushChannel;
        private readonly ConcurrentDictionary<short, IPushMessageHandler> _pushMessageHandlerDict;
        private readonly ConcurrentDictionary<int, ResponseFuture> _responseFutureDict;

        public SuperSocketSocketClient(ILogger<SuperSocketSocketClient> logger, IFluentSocketApplication app, ISocketSessionBuilder socketSessionBuilder, ClientSetting setting)
        {
            _logger = logger;
            _app = app;
            _socketSessionBuilder = socketSessionBuilder;

            _setting = setting;
            if (!_setting.ExtraSettings.Any())
            {
                _setting.ExtraSettings.Add(new SuperSocketClientSetting());
            }
            _extraSetting = (SuperSocketClientSetting)_setting.ExtraSettings.FirstOrDefault();

            _cts = new CancellationTokenSource();

            _semaphoreSlim = new SemaphoreSlim(1);
            _manualResetEventSlim = new ManualResetEventSlim(true);
            _reqPushChannel = SystemChannel.CreateBounded<PushReqPacket>(_setting.PushReqCapacity);
            _pushMessageHandlerDict = new ConcurrentDictionary<short, IPushMessageHandler>();
            _responseFutureDict = new ConcurrentDictionary<int, ResponseFuture>();
        }

        /// <summary>Connect to server
        /// </summary>
        public async ValueTask ConnectAsync()
        {
            if (_isConnected)
            {
                _logger.LogWarning("Socket client is connected , don't connect again !");
                return;
            }
            try
            {
                _easyClient = new EasyClient<Packet>(new PacketFilter(9));
                await _easyClient.ConnectAsync(_setting.ServerEndPoint);

                _isConnected = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Socket client to server '{0}' fail!", _setting.ServerEndPoint.ToStringAddress());
                _isConnected = false;
            }
        }


        public ValueTask CloseAsync()
        {
            throw new NotImplementedException();
        }


        public void RegisterPushHandler(short code, IPushMessageHandler handler)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ResponseMessage> SendMessageAsync(RequestMessage request, int timeoutMillis = 3000)
        {
            throw new NotImplementedException();
        }
    }
}
