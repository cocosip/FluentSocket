using FluentSocket.Protocols;
using FluentSocket.Traffic;
using Microsoft.Extensions.Logging;
using SuperSocket.Channel;
using SuperSocket.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SystemChannels = System.Threading.Channels;
using SystemChannel = System.Threading.Channels.Channel;

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
        private int _reConnectAttempt = 0;
        private int _sequence = 1;



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

        public async ValueTask ConnectAsync()
        {
            if (_isConnected)
            {
                _logger.LogWarning("Client is connected , don't connect again !");
                return;
            }
            _easyClient = new EasyClient<Packet>(new PacketFilter(9), new ChannelOptions()
            {

            });
            
            //try
            //{
            //    //await _easyClient.ConnectAsync(EndPoint )
            //}
            //catch (Exception ex)
            //{

            //}

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
