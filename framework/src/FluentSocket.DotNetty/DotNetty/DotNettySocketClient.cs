using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using FluentSocket.DotNetty.Handlers;
using FluentSocket.Protocols;
using FluentSocket.Traffic;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FluentSocket.DotNetty
{
    public class DotNettySocketClient : ISocketClient
    {
        /// <summary>ServerEndPoint
        /// </summary>
        public IPEndPoint ServerEndPoint => _setting.ServerEndPoint;

        /// <summary>LocalEndPoint
        /// </summary>
        public IPEndPoint LocalEndPoint => _setting.LocalEndPoint;

        /// <summary>IsConnected
        /// </summary>
        public bool IsConnected => _isConnected;

        private readonly ILogger _logger;
        private readonly IFluentSocketApplication _app;
        private readonly ISocketSessionBuilder _socketSessionBuilder;
        private readonly ClientSetting _setting;

        private readonly DotNettyClientSetting _extraSetting;
        private IEventLoopGroup _group = null;
        private IChannel _clientChannel = null;
        private Bootstrap _bootStrap = null;

        private bool _isConnected = false;
        private int _reConnectAttempt = 0;
        private int _sequence = 1;

        private readonly CancellationTokenSource _cts;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ManualResetEventSlim _manualResetEventSlim;
        private readonly Channel<PushReqPacket> _reqPushChannel;
        private readonly ConcurrentDictionary<short, IPushMessageHandler> _pushMessageHandlerDict;
        private readonly ConcurrentDictionary<int, ResponseFuture> _responseFutureDict;

        public DotNettySocketClient(ILogger<DotNettySocketClient> logger, IFluentSocketApplication app, ISocketSessionBuilder socketSessionBuilder, ClientSetting setting)
        {
            _logger = logger;
            _app = app;
            _socketSessionBuilder = socketSessionBuilder;

            _setting = setting;
            if (!_setting.ExtraSettings.Any())
            {
                _setting.ExtraSettings.Add(new DotNettyClientSetting());
            }
            _extraSetting = (DotNettyClientSetting)_setting.ExtraSettings.FirstOrDefault();

            _cts = new CancellationTokenSource();

            _semaphoreSlim = new SemaphoreSlim(1);
            _manualResetEventSlim = new ManualResetEventSlim(true);
            _reqPushChannel = Channel.CreateBounded<PushReqPacket>(_setting.PushReqCapacity);
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
                _group = new MultithreadEventLoopGroup(_extraSetting.GroupEventLoopCount);
                _bootStrap = new Bootstrap();
                _bootStrap.Group(_group);
                _bootStrap
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, _extraSetting.TcpNodelay)
                    .Option(ChannelOption.SoKeepalive, _extraSetting.SoKeepalive)
                    .Option(ChannelOption.WriteBufferHighWaterMark, _extraSetting.WriteBufferHighWaterMark)
                    .Option(ChannelOption.WriteBufferLowWaterMark, _extraSetting.WriteBufferHighWaterMark)
                    .Option(ChannelOption.SoRcvbuf, _extraSetting.SoRcvbuf)
                    .Option(ChannelOption.SoSndbuf, _extraSetting.SoSndbuf)
                    .Option(ChannelOption.SoReuseaddr, _extraSetting.SoReuseaddr)
                    .Option(ChannelOption.AutoRead, _extraSetting.AutoRead)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (_extraSetting.IsSsl && _extraSetting.TlsCertificate != null)
                        {
                            //var targetHost = _setting.TlsCertificate.GetNameInfo(X509NameType.DnsName, false);

                            //pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }

                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast(new IdleStateHandler(_setting.HeartbeatInterval, _setting.HeartbeatInterval, _setting.HeartbeatInterval));
                        }

                        pipeline.AddLast(new LoggingHandler(nameof(DotNettySocketClient)));
                        pipeline.AddLast(new PacketDecoder(), new PacketEncoder());

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast("HeartbeatHandler", _app.ServiceProvider.CreateInstance<HeartbeatHandler>());
                        }

                        //Handle PongPacket
                        pipeline.AddLast("PongPacketHandler", _app.ServiceProvider.CreateInstance<PongPacketHandler>());

                        //Socket client
                        Func<PushReqPacket, ValueTask> writePushReqPacketHandler = WritePushReqPacketAsync;
                        Action<MessageRespPacket> setMessageRespPacketHandler = SetMessageRespPacket;
                        Action channelWritabilityChangedHandler = ChannelWritabilityChanged;
                        pipeline.AddLast("SocketClientHandler", _app.ServiceProvider.CreateInstance<SocketClientHandler>(writePushReqPacketHandler, setMessageRespPacketHandler, channelWritabilityChangedHandler));

                        if (_setting.EnableReConnect)
                        {
                            //Reconnect to server
                            pipeline.AddLast("ReConnectHandler", _app.ServiceProvider.CreateInstance<ReConnectHandler>(_setting, new Func<Task>(DoReConnectAsync)));
                        }

                    }));

                await DoConnectAsync();

                _isConnected = true;

                StartScanTimeoutRequestTask();
                StartHandlePushReqPacketTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                await _group?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds));
            }

        }

        /// <summary>Send message async
        /// </summary>
        public async ValueTask<ResponseMessage> SendMessageAsync(RequestMessage request, int timeoutMillis = 5000)
        {
            if (_clientChannel == null)
            {
                throw new ArgumentNullException("Socket client should connect first!");
            }
            if (!_clientChannel.IsWritable)
            {
                _manualResetEventSlim.Wait();
            }

            var sequence = Interlocked.Increment(ref _sequence);
            var messageReqPacket = new MessageReqPacket()
            {
                Sequence = sequence,
                Code = request.Code,
                Body = request.Body,
            };
            var tcs = new TaskCompletionSource<ResponseMessage>();
            var responseFuture = new ResponseFuture(request.Code, timeoutMillis, tcs);
            if (!_responseFutureDict.TryAdd(sequence, responseFuture))
            {
                throw new Exception($"Add 'ResponseFuture' failed. Sequence:{sequence}");
            }
            await _clientChannel.WriteAndFlushAsync(messageReqPacket);
            return await tcs.Task;
        }

        /// <summary>Disconnect to server
        /// </summary>
        public async ValueTask CloseAsync()
        {
            if (_clientChannel == null)
            {
                return;
            }
            try
            {
                _isConnected = false;
                _cts.Cancel(true);
                await _clientChannel.CloseAsync();
            }
            finally
            {
                await _group?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds));
            }
        }

        /// <summary>Register PushMessageHandler
        /// </summary>
        public void RegisterPushHandler(short code, IPushMessageHandler handler)
        {
            if (!_pushMessageHandlerDict.TryAdd(code, handler))
            {
                _logger.LogInformation("Register 'PushMessageHandler' fail! Code {0}", code);
            }
        }

        #region Private Methods

        /// <summary>DoConnect to the server
        /// </summary>
        private async ValueTask DoConnectAsync()
        {
            _clientChannel = _setting.LocalEndPoint == null ? await _bootStrap.ConnectAsync(_setting.ServerEndPoint) : await _bootStrap.ConnectAsync(_setting.ServerEndPoint, _setting.LocalEndPoint);

            _logger.LogInformation("Socket client connect to server:{0}.", _setting.ServerEndPoint.ToStringAddress());

            if (_setting.LocalEndPoint != null)
            {
                _logger.LogInformation("Socket client bind local:{0}.", _setting.LocalEndPoint.ToStringAddress());
            }
        }


        /// <summary>Do reconnect
        /// </summary>
        private async Task DoReConnectAsync()
        {
            if (!_isConnected)
            {
                _logger.LogDebug("Socket client is close, it will not reconnect! ChannelId:{0}", _clientChannel?.Id.AsShortText());
                return;
            }

            //Current channel is active
            if (_clientChannel.Active)
            {
                _logger.LogDebug("Socket client channel is active,it will not reconnect! ChannelId:{0}", _clientChannel?.Id.AsShortText());
                return;
            }

            //判断是否为主动关闭
            if (!_setting.EnableReConnect || _setting.ReConnectMaxCount <= _reConnectAttempt)
            {
                return;
            }
            if (_clientChannel != null && !_clientChannel.Active)
            {
                await _semaphoreSlim.WaitAsync();
                bool reConnectSuccess = false;
                try
                {
                    _logger.LogInformation("Try to reconnect server {0} !", _setting.ServerEndPoint.ToStringAddress());
                    await DoConnectAsync();
                    Interlocked.Exchange(ref _reConnectAttempt, 0);
                    reConnectSuccess = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("ReConnect fail!{0}", ex.Message);
                }
                finally
                {
                    if (!reConnectSuccess)
                    {
                        Interlocked.Increment(ref _reConnectAttempt);
                    }
                    _semaphoreSlim.Release();
                }
                //Try again!
                if (_reConnectAttempt < _setting.ReConnectMaxCount && !reConnectSuccess)
                {
                    await Task.Delay(_setting.ReConnectDelaySeconds);
                    await DoReConnectAsync();
                }
            }
        }

        /// <summary>Scan sended 'ReqMessagePacket' whether it is timeout
        /// </summary>
        private void StartScanTimeoutRequestTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var timeoutKeyList = new List<int>();
                        foreach (var entry in _responseFutureDict)
                        {
                            if (entry.Value.IsTimeout())
                            {
                                timeoutKeyList.Add(entry.Key);
                            }
                        }
                        foreach (var key in timeoutKeyList)
                        {
                            if (_responseFutureDict.TryRemove(key, out ResponseFuture responseFuture))
                            {
                                var timeoutResp = new ResponseMessage()
                                {
                                    Code = responseFuture.Code,
                                };
                                responseFuture.SetResponse(timeoutResp);
                                _logger.LogDebug("Removed timeout request, sequence:{0}", _sequence);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Scan timeout request fail,ex:{0}", ex.Message);
                    }
                    finally
                    {
                        await Task.Delay(_setting.ScanTimeoutRequestInterval);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>Loop handle 'ReqPushPacket'
        /// </summary>
        private void StartHandlePushReqPacketTask()
        {
            for (int i = 0; i < _setting.HandlePushReqThread; i++)
            {
                Task.Factory.StartNew(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await HandleReqPushPacketAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Handle ReqMessagePacket fail,ex:{0}", ex.Message);
                            await Task.Delay(1000);
                        }
                    }

                }, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>Handle the 'ReqPushPacket' received from server
        /// </summary>
        private async ValueTask HandleReqPushPacketAsync()
        {
            var pushReqPacket = await _reqPushChannel.Reader.ReadAsync(_cts.Token);
            if (!_pushMessageHandlerDict.TryGetValue(pushReqPacket.Code, out IPushMessageHandler handler))
            {
                _logger.LogWarning("Can't find any 'IPushMessageHandler' from the dict by code '{0}'! ", pushReqPacket.Code);
                return;
            }

            var request = new RequestPush()
            {
                Code = pushReqPacket.Code,
                Body = pushReqPacket.Body
            };

            var response = await handler.HandlePushAsync(request);
            switch (pushReqPacket.PushType)
            {
                case PushType.NoReply:
                case PushType.Unknow:
                    break;
                case PushType.Reply:
                    var respPushMessagePacket = new PushRespPacket()
                    {
                        Sequence = pushReqPacket.Sequence,
                        PushType = pushReqPacket.PushType,
                        Code = response.Code,
                        Body = response.Body
                    };
                    await _clientChannel.WriteAndFlushAsync(respPushMessagePacket);
                    break;
                default:
                    break;
            }
        }

        /// <summary>Write the 'Message'
        /// </summary>
        private ValueTask WritePushReqPacketAsync(PushReqPacket packet)
        {
            return _reqPushChannel.Writer.WriteAsync(packet);
        }

        /// <summary>Handle the 'RespMessagePacket' received from server
        /// </summary>
        private void SetMessageRespPacket(MessageRespPacket packet)
        {
            if (_responseFutureDict.TryRemove(packet.Sequence, out ResponseFuture responseFuture))
            {
                var responseMessage = new ResponseMessage()
                {
                    Code = packet.Code,
                    Body = packet.Body
                };

                if (!responseFuture.SetResponse(responseMessage))
                {
                    _logger.LogDebug("Set remoting response failed,Sequence: '{0}'.", packet.Sequence);
                }
            }
            else
            {
                _logger.LogDebug("Try remove  from responseFuture dict fail!");
            }
        }


        private void ChannelWritabilityChanged()
        {
            if (_clientChannel.IsWritable)
            {
                _manualResetEventSlim.Set();
            }
            else
            {
                _manualResetEventSlim.Reset();
            }
        }
        #endregion

    }
}
