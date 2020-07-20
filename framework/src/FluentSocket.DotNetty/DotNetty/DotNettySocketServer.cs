using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using DotNetty.Transport.Channels.Sockets;
using FluentSocket.DotNetty.Handlers;
using FluentSocket.Impl;
using FluentSocket.Protocols;
using FluentSocket.Traffic;
using Microsoft.Extensions.DependencyInjection;
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
    public class DotNettySocketServer : ISocketServer
    {
        /// <summary>Listening ip address and port
        /// </summary>
        public IPEndPoint ListeningEndPoint => _setting.ListeningEndPoint;

        /// <summary>IsRunning
        /// </summary>
        public bool IsRunning => _isRunning;

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerSetting _setting;
        private readonly DotNettyServerSetting _extraSetting;

        private readonly ISocketSessionFactory _sessionFactory;

        private IEventLoopGroup _bossGroup = null;
        private IEventLoopGroup _workerGroup = null;
        private IChannel _boundChannel = null;
        private IChannelGroup _channelGroup = null;

        private bool _isRunning = false;
        private int _sequence = 1;

        private readonly CancellationTokenSource _cts;
        private readonly Channel<ReqMessagePacketWrapper> _reqMessageChannel;
        private readonly ConcurrentDictionary<short, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ConcurrentDictionary<int, PushFuture> _pushFutureDict;
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

            _sessionFactory = _serviceProvider.GetService<ISocketSessionFactory>();

            _cts = new CancellationTokenSource();
            _reqMessageChannel = Channel.CreateBounded<ReqMessagePacketWrapper>(_setting.ReqPacketChannelCapacity);

            _requestMessageHandlerDict = new ConcurrentDictionary<short, IRequestMessageHandler>();
            _pushFutureDict = new ConcurrentDictionary<int, PushFuture>();
        }

        /// <summary>Run socket server
        /// </summary>
        public async ValueTask RunAsync()
        {
            if (_boundChannel != null)
            {
                _logger.LogInformation($"Server is running! Don't run again! ChannelId:{_boundChannel.Id.AsShortText()}");
                return;
            }
            _bossGroup = new MultithreadEventLoopGroup(_extraSetting.BossGroupEventLoopCount);
            _workerGroup = new MultithreadEventLoopGroup(_extraSetting.WorkGroupEventLoopCount);

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, _extraSetting.SoBacklog)
                    .ChildOption(ChannelOption.TcpNodelay, _extraSetting.TcpNodelay)
                    .ChildOption(ChannelOption.WriteBufferHighWaterMark, _extraSetting.WriteBufferHighWaterMark)
                    .ChildOption(ChannelOption.WriteBufferLowWaterMark, _extraSetting.WriteBufferLowWaterMark)
                    .ChildOption(ChannelOption.SoRcvbuf, _extraSetting.SoRcvbuf)
                    .ChildOption(ChannelOption.SoSndbuf, _extraSetting.SoSndbuf)
                    .ChildOption(ChannelOption.SoReuseaddr, _extraSetting.SoReuseaddr)
                    .ChildOption(ChannelOption.AutoRead, _extraSetting.AutoRead)
                    .Handler(new LoggingHandler(nameof(DotNettySocketServer)))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        //if (_setting.IsSsl && _setting.TlsCertificate != null)
                        //{
                        //    pipeline.AddLast("tls", TlsHandler.Server(_setting.TlsCertificate));
                        //}

                        //Server channel manager
                        //pipeline.AddLast("channel-manager", _provider.CreateInstance<ServerChannelManagerHandler>(_channelManager, _setting.OnChannelActiveHandler, _setting.OnChannelInActiveHandler));
                        //RemotingMessage coder and encoder
                        pipeline.AddLast(new PacketDecoder(), new PacketEncoder());

                        //Handle PingPacket
                        pipeline.AddLast("PingPacketHandler", _serviceProvider.CreateInstance<PingPacketHandler>());


                        //Handle RespPushPacket
                        pipeline.AddLast("RespPushPacketHandler", _serviceProvider.CreateInstance<RespPushPacketHandler>(new Action<RespPushPacket>(HandleRespPushPacket)));

                        //Handle ReqMessagePacket
                        pipeline.AddLast("ReqMessagePacketHandler", _serviceProvider.CreateInstance<ReqPacketHandler>(new Func<string, ReqMessagePacket, ValueTask>(WriteReqPacketAsync)));

                    }));


                _boundChannel = await bootstrap.BindAsync(_setting.ListeningEndPoint);
                _isRunning = true;

                _logger.LogInformation($"Server Run! ListeningEndPoint:{_setting.ListeningEndPoint}, boundChannel:{_boundChannel.Id.AsShortText()}");


                StartScanTimeoutRequestTask();

                StartHandleReqPacketTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server run failed,ex:{0}.", ex.Message);
                await Task.WhenAll(
                    _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)),
                    _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)));
            }
        }

        /// <summary>Send message async
        /// </summary>
        public async ValueTask<ResponsePush> PushAsync(RequestPush request, ISocketSession session, int timeoutMillis = 5000)
        {
            if (_boundChannel == null)
            {
                throw new ArgumentNullException("Server should run first!");
            }
            //if (!_boundChannel.IsWritable)
            //{
            //    _manualResetEventSlim.Wait();
            //}

            var sequence = Interlocked.Increment(ref _sequence);
            var messageReqPacket = new ReqMessagePacket()
            {
                Code = request.Code,
                Body = request.Body,
            };
            var tcs = new TaskCompletionSource<ResponsePush>();
            //var responseFuture = new ResponseFuture(request.Code, timeoutMillis, tcs);
            //if (!_pushFutureDict.TryAdd(sequence, responseFuture))
            //{
            //    throw new Exception($"Add 'ResponseFuture' failed. Sequence:{sequence}");
            //}
            //await _clientChannel.WriteAndFlushAsync(messageReqPacket);
            return await tcs.Task;
        }

        /// <summary>Register RequestHandler
        /// </summary>
        public void RegisterRequestHandler(short code, IRequestMessageHandler handler)
        {
            if (!_requestMessageHandlerDict.TryAdd(code, handler))
            {
                _logger.LogInformation("Register RequestMessageHandler fail! Code {0}", code);
            }
        }

        #region Private Methods

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
                        foreach (var entry in _pushFutureDict)
                        {
                            if (entry.Value.IsTimeout())
                            {
                                timeoutKeyList.Add(entry.Key);
                            }
                        }
                        foreach (var key in timeoutKeyList)
                        {
                            if (_pushFutureDict.TryRemove(key, out PushFuture pushFuture))
                            {
                                var timeoutResp = new ResponsePush()
                                {
                                    Code = pushFuture.Code,
                                    PushType = PushType.Reply
                                };
                                pushFuture.SetResponse(timeoutResp);
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
            for (int i = 0; i < _extraSetting.HandleReqThreadCount; i++)
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



        /// <summary>Write the 'Message'
        /// </summary>
        private ValueTask WriteReqPacketAsync(string sessionId, ReqMessagePacket packet)
        {
            var session = _sessionFactory.GetSession(sessionId);
            if (session == null)
            {
                _logger.LogError("Can't find session by sessionId '{0}'!", sessionId);
                return new ValueTask();
            }
            var wrapper = new ReqMessagePacketWrapper()
            {
                Session = session,
                Packet = packet
            };
            return _reqMessageChannel.Writer.WriteAsync(wrapper);
        }

        private async ValueTask HandleReqPacketAsync()
        {
            var wrapper = await _reqMessageChannel.Reader.ReadAsync(_cts.Token);
            if (wrapper.Packet == null)
            {
                _logger.LogError("Read packet is null.");
                return;
            }

            if (!_requestMessageHandlerDict.TryGetValue(wrapper.Packet.Code, out IRequestMessageHandler requestMessageHandler))
            {
                _logger.LogError("Can't find any 'IRequestMessageHandler' from the dict by code '{0}'! ", wrapper.Packet.Code);
                return;
            }

            var request = new RequestMessage()
            {
                Code = wrapper.Packet.Code,
                Body = wrapper.Packet.Body
            };

            var response = await requestMessageHandler.HandleRequestAsync(wrapper.Session, request);

            var respMessagePacket = new RespMessagePacket()
            {
                Sequence = wrapper.Packet.Sequence,
                Code = response.Code,
                Body = response.Body
            };

            //await _clientChannel.WriteAndFlushAsync(respMessagePacket);
        }

        /// <summary>Handle the 'RespPushPacket' received from client
        /// </summary>
        private void HandleRespPushPacket(RespPushPacket packet)
        {
            if (_pushFutureDict.TryRemove(packet.Sequence, out PushFuture pushFuture))
            {
                var responsePush = new ResponsePush()
                {
                    PushType = PushType.Reply,
                    Code = packet.Code,
                    Body = packet.Body
                };

                if (!pushFuture.SetResponse(responsePush))
                {
                    _logger.LogDebug("Set remoting response failed,Sequence: '{0}'.", packet.Sequence);
                }
            }
        }

        #endregion

    }
}
