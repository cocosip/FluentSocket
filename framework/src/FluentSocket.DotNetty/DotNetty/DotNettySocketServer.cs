using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
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
        private readonly IFluentSocketApplication _app;
        private readonly ServerSetting _setting;
        private readonly DotNettyServerSetting _extraSetting;

        private readonly ISocketSessionBuilder _socketSessionBuilder;
        private readonly ISocketSessionFactory _sessionFactory;

        private IEventLoopGroup _bossGroup = null;
        private IEventLoopGroup _workerGroup = null;
        private IChannel _boundChannel = null;

        private bool _isRunning = false;
        private int _sequence = 1;

        private readonly CancellationTokenSource _cts;
        private readonly Channel<MessageReqPacketWrapper> _messageReqChannel;
        private readonly ConcurrentDictionary<short, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ConcurrentDictionary<int, PushFuture> _pushFutureDict;
        private readonly ConcurrentDictionary<string, IChannel> _channelDict;
        public DotNettySocketServer(ILogger<DotNettySocketServer> logger, IFluentSocketApplication app, ISocketSessionBuilder socketSessionBuilder, ServerSetting setting)
        {
            _logger = logger;
            _app = app;

            _setting = setting;
            if (!_setting.ExtraSettings.Any())
            {
                _setting.ExtraSettings.Add(new DotNettyServerSetting());
            }
            _extraSetting = (DotNettyServerSetting)_setting.ExtraSettings.FirstOrDefault();

            _socketSessionBuilder = socketSessionBuilder;
            _sessionFactory = app.ServiceProvider.GetService<ISocketSessionFactory>();

            _cts = new CancellationTokenSource();
            _messageReqChannel = Channel.CreateBounded<MessageReqPacketWrapper>(_setting.MessageReqCapacity);

            _requestMessageHandlerDict = new ConcurrentDictionary<short, IRequestMessageHandler>();
            _pushFutureDict = new ConcurrentDictionary<int, PushFuture>();
            _channelDict = new ConcurrentDictionary<string, IChannel>();
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

                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));

                        //coder and encoder
                        pipeline.AddLast(new PacketDecoder(), new PacketEncoder());

                        //Handle PingPacket
                        pipeline.AddLast("PingPacketHandler", _app.ServiceProvider.CreateInstance<PingPacketHandler>());

                        //SocketServer handler
                        Func<string, MessageReqPacket, ValueTask> writeMessageReqPacketHandler = WriteMessageReqPacketAsync;
                        Action<PushRespPacket> setPushRespPacketHandler = SetPushRespPacket;
                        Action<IChannel, bool> channelActiveInActiveHandler = ActiveInActiveHandler;

                        pipeline.AddLast("SocketServerHandler", _app.ServiceProvider.CreateInstance<SocketServerHandler>(writeMessageReqPacketHandler, setPushRespPacketHandler, channelActiveInActiveHandler));

                    }));

                _boundChannel = await bootstrap.BindAsync(_setting.ListeningEndPoint);
                _isRunning = true;

                _logger.LogInformation($"Server Run! ListeningEndPoint:{_setting.ListeningEndPoint}, boundChannel:{_boundChannel.Id.AsShortText()}");

                StartScanTimeoutRequestTask();
                StartHandleMessageReqPacketTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server run failed,ex:{0}.", ex.Message);
                await Task.WhenAll(
                    _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)),
                    _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)));
            }
        }

        /// <summary>Push message async
        /// </summary>
        public async ValueTask<ResponsePush> PushAsync(RequestPush request, ISocketSession session, int timeoutMillis = 5000)
        {
            if (_boundChannel == null)
            {
                throw new ArgumentNullException("Server should run first!");
            }

            //Get channel from dict
            if (!_channelDict.TryGetValue(session.SessionId, out IChannel channel))
            {
                throw new ArgumentNullException("Can't find session '{0}' channel.", session.SessionId);
            }

            var sequence = Interlocked.Increment(ref _sequence);
            var pushReqPacket = new PushReqPacket()
            {
                Sequence = sequence,
                PushType = PushType.Reply,
                Code = request.Code,
                Body = request.Body,
            };
            var tcs = new TaskCompletionSource<ResponsePush>();
            var pushFuture = new PushFuture(request.Code, timeoutMillis, tcs);
            if (!_pushFutureDict.TryAdd(sequence, pushFuture))
            {
                throw new Exception($"Add 'PushFuture' failed. Sequence:{sequence}");
            }
            await channel.WriteAndFlushAsync(pushReqPacket);
            return await tcs.Task;
        }

        /// <summary>Server close
        /// </summary>
        public async ValueTask CloseAsync()
        {
            if (_boundChannel == null)
            {
                return;
            }
            try
            {
                _isRunning = false;
                _cts.Cancel(true);
                await _boundChannel.CloseAsync();
            }
            finally
            {
                _logger.LogInformation("Server close!");
                await Task.WhenAll(
                    _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)),
                    _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)));

                _boundChannel = null;
                _bossGroup = null;
                _workerGroup = null;
            }
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

        /// <summary>Get sessions
        /// </summary>
        public List<ISocketSession> GetSessions(Func<ISocketSession, bool> predicate = null)
        {
            return predicate == null ? _sessionFactory.GetAllSessions() : _sessionFactory.GetSessions(predicate);
        }

        #region Private Methods

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

        private void StartHandleMessageReqPacketTask()
        {
            for (int i = 0; i < _setting.HandleMessageReqThread; i++)
            {
                Task.Factory.StartNew(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await HandleMessageReqPacketAsync();
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

        private async ValueTask HandleMessageReqPacketAsync()
        {
            var wrapper = await _messageReqChannel.Reader.ReadAsync(_cts.Token);
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
            
            if (!_channelDict.TryGetValue(wrapper.Session.SessionId, out IChannel channel))
            {
                _logger.LogError("Can't find any channel by session {0}", wrapper?.Session.SessionId);
                return;
            }

            var messageRespPacket = new MessageRespPacket()
            {
                Sequence = wrapper.Packet.Sequence,
                Code = response.Code,
                Body = response.Body
            };

            if (!channel.IsWritable)
            {
                _logger.LogInformation("Channel not writeable!");
            }

            await channel.WriteAndFlushAsync(messageRespPacket);
        }

        /// <summary>Write 'MessageReqPacket' to channel
        /// </summary>
        private ValueTask WriteMessageReqPacketAsync(string sessionId, MessageReqPacket packet)
        {
            var session = _sessionFactory.GetSession(sessionId);
            if (session == null)
            {
                _logger.LogError("Can't find session by sessionId '{0}'!", sessionId);
                return new ValueTask();
            }
            var wrapper = new MessageReqPacketWrapper()
            {
                Session = session,
                Packet = packet
            };
            return _messageReqChannel.Writer.WriteAsync(wrapper);
        }

        /// <summary>Handle the 'RespPushPacket' received from client
        /// </summary>
        private void SetPushRespPacket(PushRespPacket packet)
        {
            if (packet.PushType != PushType.Reply)
            {
                _logger.LogInformation("PushRespPacket type is not 'Reply' , will not set response !");
                return;
            }

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

        /// <summary>Active InActive handler
        /// </summary>
        private void ActiveInActiveHandler(IChannel channel, bool active)
        {
            if (active)
            {
                if (!_channelDict.ContainsKey(channel.Id.AsLongText()))
                {
                    _channelDict.GetOrAdd(channel.Id.AsLongText(), channel);

                    var session = _socketSessionBuilder.BuildSession(channel.Id.AsLongText(), channel.RemoteAddress, channel.LocalAddress, SocketSessionState.Connected);
                    _sessionFactory.AddOrUpdateSession(session);
                }
            }
            else
            {
                if (!_channelDict.TryRemove(channel.Id.AsLongText(), out IChannel _))
                {
                    _logger.LogDebug("Remove channel from dict fail! channel id '{0}'", channel.Id.AsLongText());
                }
                _sessionFactory.RemoveSession(channel.Id.AsLongText());
            }
        }

        #endregion

    }
}
