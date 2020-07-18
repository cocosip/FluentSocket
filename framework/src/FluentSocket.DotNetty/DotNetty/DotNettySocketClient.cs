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
        private readonly IServiceProvider _serviceProvider;
        private readonly ClientSetting _setting;

        private readonly DotNettyClientSetting _dotNettyClientSetting;
        private IEventLoopGroup _group = null;
        private IChannel _clientChannel = null;
        private Bootstrap _bootStrap = null;
        private bool _isConnected = false;
        private int _reConnectAttempt = 0;
        private int _sequence = 1;

        private readonly CancellationTokenSource _cts;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ManualResetEventSlim _manualResetEventSlim;
        private readonly Channel<ReqMessagePacket> _messageReqChannel;
        private readonly ConcurrentDictionary<short, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ConcurrentDictionary<int, ResponseFuture> _responseFutureDict;

        public DotNettySocketClient(ILogger<DotNettySocketClient> logger, IServiceProvider serviceProvider, ClientSetting setting)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            _setting = setting;
            if (!_setting.ExtraSettings.Any())
            {
                _setting.ExtraSettings.Add(new DotNettyClientSetting());
            }
            _dotNettyClientSetting = (DotNettyClientSetting)_setting.ExtraSettings.FirstOrDefault();

            _cts = new CancellationTokenSource();

            _semaphoreSlim = new SemaphoreSlim(1);
            _manualResetEventSlim = new ManualResetEventSlim(false);
            _messageReqChannel = Channel.CreateBounded<ReqMessagePacket>(_setting.ReqPacketChannelCapacity);
            _requestMessageHandlerDict = new ConcurrentDictionary<short, IRequestMessageHandler>();
            _responseFutureDict = new ConcurrentDictionary<int, ResponseFuture>();
        }

        /// <summary>Connect to server
        /// </summary>
        public async ValueTask ConnectAsync()
        {
            if (_isConnected)
            {
                _logger.LogWarning("Client is connected , don't connect again !");
                return;
            }

            try
            {
                _group = new MultithreadEventLoopGroup(_dotNettyClientSetting.GroupEventLoopCount);
                _bootStrap = new Bootstrap();
                _bootStrap.Group(_group);
                _bootStrap
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, _dotNettyClientSetting.TcpNodelay)
                    .Option(ChannelOption.SoKeepalive, _dotNettyClientSetting.SoKeepalive)
                    .Option(ChannelOption.WriteBufferHighWaterMark, _dotNettyClientSetting.WriteBufferHighWaterMark)
                    .Option(ChannelOption.WriteBufferLowWaterMark, _dotNettyClientSetting.WriteBufferHighWaterMark)
                    .Option(ChannelOption.SoRcvbuf, _dotNettyClientSetting.SoRcvbuf)
                    .Option(ChannelOption.SoSndbuf, _dotNettyClientSetting.SoSndbuf)
                    .Option(ChannelOption.SoReuseaddr, _dotNettyClientSetting.SoReuseaddr)
                    .Option(ChannelOption.AutoRead, _dotNettyClientSetting.AutoRead)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (_dotNettyClientSetting.IsSsl && _dotNettyClientSetting.TlsCertificate != null)
                        {
                            //var targetHost = _setting.TlsCertificate.GetNameInfo(X509NameType.DnsName, false);

                            //pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast(new IdleStateHandler(_setting.HeartbeatInterval, _setting.HeartbeatInterval, _setting.HeartbeatInterval));
                        }

                        pipeline.AddLast(new LoggingHandler(nameof(DotNettySocketClient)));
                        pipeline.AddLast(new PacketDecoder(), new PacketEncoder());

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast("HeartbeatHandler", _serviceProvider.CreateInstance<HeartbeatHandler>());
                        }

                        //Handle PongPacket
                        pipeline.AddLast("PongPacketHandler", _serviceProvider.CreateInstance<PongPacketHandler>());

                        //Handler Packet
                        Action<RespMessagePacket> handleRespPacketHandler = HandleRespPacket;
                        Func<ReqMessagePacket, ValueTask> writeReqPacketHandler = WriteReqPacket;
                        pipeline.AddLast("PacketHandler", _serviceProvider.CreateInstance<PacketHandler>(handleRespPacketHandler, writeReqPacketHandler));

                        Action<bool> channelWritabilityChangedHandler = ChannelWritabilityChanged;
                        pipeline.AddLast("WritabilityChangedHandler", _serviceProvider.CreateInstance<ChannelWritabilityChangedHandler>(channelWritabilityChangedHandler));

                        //AddPushMessageHandler(pipeline);

                        if (_setting.EnableReConnect)
                        {
                            //Reconnect to server
                            pipeline.AddLast("ReConnectHandler", _serviceProvider.CreateInstance<ReConnectHandler>(_setting, new Func<Task>(DoReConnectAsync)));
                        }

                    }));

                if (_setting.LocalEndPoint == null)
                {
                    _clientChannel = await _bootStrap.ConnectAsync(_setting.ServerEndPoint);
                }
                else
                {
                    _clientChannel = await _bootStrap.ConnectAsync(_setting.ServerEndPoint, _setting.LocalEndPoint);
                }

                await DoConnectAsync();

                _isConnected = true;

                StartScanTimeoutRequestTask();

                StartHandleReqPacketTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                await _group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds));
            }

        }

        /// <summary>Send message async
        /// </summary>
        public async ValueTask<ResponseMessage> SendMessageAsync(RequestMessage request, int timeoutMillis = 3000)
        {
            if (_clientChannel == null)
            {
                throw new ArgumentNullException("Client should connect first!");
            }
            if (!_clientChannel.IsWritable)
            {
                _manualResetEventSlim.Wait();
            }

            var sequence = Interlocked.Increment(ref _sequence);
            var messageReqPacket = new ReqMessagePacket()
            {
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

                _clientChannel = null;
                _group = null;
                _bootStrap = null;
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

        #region Private Methods

        /// <summary>DoConnect to the server
        /// </summary>
        private async Task DoConnectAsync()
        {
            _clientChannel = _setting.LocalEndPoint == null ? await _bootStrap.ConnectAsync(_setting.ServerEndPoint) : await _bootStrap.ConnectAsync(_setting.ServerEndPoint, _setting.LocalEndPoint);
            _logger.LogInformation($"Client DoConnect! ServerEndPoint:{_clientChannel.RemoteAddress.ToStringAddress()},LocalEndPoint:{_clientChannel.LocalAddress.ToStringAddress()}");
        }

        /// <summary>Do reconnect
        /// </summary>
        private async Task DoReConnectAsync()
        {
            //If active close,it will not reconnect!
            if (!_isConnected)
            {
                _logger.LogDebug("Current channel is close,it will not reconnect! channel id:{0}", _clientChannel?.Id.AsShortText());
                return;
            }
            //Current channel is active
            if (_clientChannel.Active)
            {
                _logger.LogDebug("Current channel is close,it will not reconnect! channel id:{0}", _clientChannel?.Id.AsShortText());
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
                    _logger.LogInformation($"Try to reconnect server!");
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
                while (_cts.Token.IsCancellationRequested)
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

        private void StartHandleReqPacketTask()
        {
            for (int i = 0; i < _dotNettyClientSetting.HandleReqThreadCount; i++)
            {
                Task.Factory.StartNew(async () =>
                {
                    while (_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await HandleReqPacketAsync();
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

        /// <summary>Handle the 'MessageRespPacket' received from server
        /// </summary>
        private void HandleRespPacket(RespMessagePacket packet)
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
        }

        /// <summary>Write the 'Message'
        /// </summary>
        private ValueTask WriteReqPacket(ReqMessagePacket packet)
        {
            return _messageReqChannel.Writer.WriteAsync(packet);
        }

        private async ValueTask HandleReqPacketAsync()
        {
            var reqMessagePacket = await _messageReqChannel.Reader.ReadAsync(_cts.Token);
            if (!_requestMessageHandlerDict.TryGetValue(reqMessagePacket.Code, out IRequestMessageHandler requestMessageHandler))
            {
                _logger.LogError("Can't find any 'IRequestMessageHandler' from the dict by code '{0}'! ", reqMessagePacket.Code);
                return;
            }

            var request = new RequestMessage()
            {
                Code = reqMessagePacket.Code,
                Body = reqMessagePacket.Body
            };

            var response = await requestMessageHandler.HandleRequestAsync(request);

            var respMessagePacket = new RespMessagePacket()
            {
                Sequence = reqMessagePacket.Sequence,
                Code = response.Code,
                Body = response.Body
            };

            await _clientChannel.WriteAndFlushAsync(respMessagePacket);
        }

        private void ChannelWritabilityChanged(bool isWriteable)
        {
            if (isWriteable)
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
