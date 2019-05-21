using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using FluentSocket.Channels;
using FluentSocket.Codecs;
using FluentSocket.Extensions;
using FluentSocket.Handlers;
using FluentSocket.Protocols;
using FluentSocket.Traffic;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket
{
    public class SocketServer
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly IScheduleService _scheduleService;
        private readonly IChannelManager _channelManager;
        private readonly ServerSetting _setting;

        public string Name { get; }
        public string LocalIPAddress { get; }
        private IEventLoopGroup _bossGroup = null;
        private IEventLoopGroup _workerGroup = null;
        private IChannel _boundChannel;
        private readonly IDictionary<int, IRequestMessageHandler> _requestMessageHandlerDict;
        private readonly ConcurrentDictionary<string, PushResponseFuture> _pushResponseFutureDict;

        public SocketServer(IServiceProvider provider, ILoggerFactory loggerFactory, IScheduleService scheduleService, IChannelManager channelManager, ServerSetting setting)
        {
            _provider = provider;
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _scheduleService = scheduleService;
            _channelManager = channelManager;
            _setting = setting;

            Name = "SocketServer-" + ObjectId.GenerateNewStringId();
            LocalIPAddress = _setting.ListeningEndPoint.ToStringAddress();

            _requestMessageHandlerDict = new Dictionary<int, IRequestMessageHandler>();
            _requestMessageHandlerDict.Add(RequestCodes.HeartBeat, _provider.CreateInstance<HeartbeatRequestMessageHander>());
            _pushResponseFutureDict = new ConcurrentDictionary<string, PushResponseFuture>();
        }

        /// <summary>Run
        /// </summary>
        public async Task RunAsync()
        {
            if (_boundChannel != null && _boundChannel.Registered)
            {
                _logger.LogInformation($"Server is running! Don't run again! ChannelId:{_boundChannel.Id.AsLongText()}");
                return;
            }

            if (_setting.UseLibuv)
            {
                var dispatcher = new DispatcherEventLoopGroup();
                _bossGroup = dispatcher;
                _workerGroup = new WorkerEventLoopGroup(dispatcher);
            }
            else
            {
                _bossGroup = new MultithreadEventLoopGroup(_setting.BossGroupEventLoopCount);
                _workerGroup = new MultithreadEventLoopGroup(_setting.WorkGroupEventLoopCount);
            }

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(_bossGroup, _workerGroup);

                if (_setting.UseLibuv)
                {
                    bootstrap.Channel<TcpServerChannel>();
                }
                else
                {
                    bootstrap.Channel<TcpServerSocketChannel>();
                }

                bootstrap
                    .Option(ChannelOption.SoBacklog, _setting.SoBacklog)
                    .ChildOption(ChannelOption.TcpNodelay, _setting.TcpNodelay)
                    .ChildOption(ChannelOption.WriteBufferHighWaterMark, _setting.WriteBufferHighWaterMark)
                    .ChildOption(ChannelOption.WriteBufferLowWaterMark, _setting.WriteBufferLowWaterMark)
                    .ChildOption(ChannelOption.SoRcvbuf, _setting.SoRcvbuf)
                    .ChildOption(ChannelOption.SoSndbuf, _setting.SoSndbuf)
                    .ChildOption(ChannelOption.SoReuseaddr, _setting.SoReuseaddr)
                    .ChildOption(ChannelOption.AutoRead, _setting.AutoRead)
                    .Handler(new LoggingHandler(FluentSocketSettings.LoggerName))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (_setting.IsSsl && _setting.TlsCertificate != null)
                        {
                            pipeline.AddLast("tls", TlsHandler.Server(_setting.TlsCertificate));
                        }
                        pipeline.AddLast(new LoggingHandler(FluentSocketSettings.LoggerName));
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast(new IdleStateHandler(_setting.ReaderIdleTimeSeconds, _setting.WriterIdleTimeSeconds, _setting.AllIdleTimeSeconds));
                        }
                        //Server channel manager
                        pipeline.AddLast("channel-manager", _provider.CreateInstance<ServerChannelManagerHandler>(_channelManager));
                        //RemotingMessage coder and encoder
                        pipeline.AddLast(new MessageDecoder(), new MessageEncoder());
                        //Hearbeat sender
                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast("heartbeat", _provider.CreateInstance<HeartbeatHandler>());
                        }

                        //Handle the response message
                        pipeline.AddLast("push-response", _provider.CreateInstance<PushResponseMessageHandler>(new Action<PushResponseMessage>(HandlePushResponseMessage)));

                        //Add request message handlers
                        AddRequestMessageHandler(pipeline);

                        //If the pipeline configure is not null,configure the pipeline
                        _setting.PipelineConfigure?.Invoke(pipeline);

                    }));

                _boundChannel = await bootstrap.BindAsync(_setting.ListeningEndPoint);

                _logger.LogInformation($"Server Run! name:{Name}, listeningEndPoint:{_setting.ListeningEndPoint}, boundChannel:{_boundChannel.Id.AsShortText()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                StartScanTimeoutPushMessageTask();
                await Task.WhenAll(
                    _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)),
                    _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)));
            }
        }

        /// <summary>Close
        /// </summary>
        public async Task CloseAsync()
        {
            if (_boundChannel == null)
            {
                return;
            }

            try
            {
                await _boundChannel.CloseAsync();
            }
            finally
            {
                StopScanTimeoutPushMessageTask();
                await Task.WhenAll(
                    _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)),
                    _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds)));
            }
        }

        /// <summary>Push message to single client
        /// </summary>
        public Task<PushResponseMessage> PushMessageToSingleClientAsync(PushMessage pushMessage, Func<ChannelInfo, bool> predicate, int timeoutMillis)
        {
            var channel = _channelManager.FindFirstChannel(predicate);
            CheckChannel(channel);
            while (!channel.IsWritable)
            {
                Thread.Sleep(50);
            }
            var taskCompletionSource = new TaskCompletionSource<PushResponseMessage>();
            var pushResponseFuture = new PushResponseFuture(pushMessage, timeoutMillis, taskCompletionSource);

            if (!_pushResponseFutureDict.TryAdd(pushMessage.Id, pushResponseFuture))
            {
                throw new Exception($"Add remoting push response future failed. push message id:{pushMessage.Id}");
            }
            channel.WriteAndFlushAsync(pushMessage).Wait();
            return taskCompletionSource.Task;
        }

        /// <summary>Push message to multiple client
        /// </summary>
        public Task PushMessageToMultipleClientAsync(PushMessage pushMessage, Func<ChannelInfo, bool> predicate, int timeoutMillis)
        {
            //push to one client need ack
            pushMessage.NeedAck = false;

            var channels = _channelManager.FindChannels(predicate);
            foreach (var channel in channels)
            {
                if (channel.IsWritable)
                {
                    channel.WriteAndFlushAsync(pushMessage).Wait(timeoutMillis);
                }
            }
            return Task.FromResult(true);
        }

        /// <summary>Register RequestMessageHandler
        /// </summary>
        public SocketServer RegisterRequestMessageHandler(int code, IRequestMessageHandler requestMessageHandler)
        {
            _requestMessageHandlerDict[code] = requestMessageHandler;
            return this;
        }

        private void CheckChannel(IChannel channel)
        {
            if (channel == null)
            {
                throw new ArgumentException("Channel is null.");
            }
            if (!channel.Open || !channel.Active)
            {
                throw new Exception($"Current channel is not useable,channelId:{channel.Id.AsShortText()}");
            }
        }

        private void AddRequestMessageHandler(IChannelPipeline pipeline)
        {
            //Some default requestCodeHandler
            _setting.RequestMessageHandlerConfigure?.Invoke(_requestMessageHandlerDict);

            var requestMessageHandler = _provider.CreateInstance<RequestHandler>(_setting);
            foreach (var item in _requestMessageHandlerDict)
            {
                requestMessageHandler.RegisterRequestHandler(item.Key, item.Value);
            }
            pipeline.AddLast("request", requestMessageHandler);
        }


        private void StartScanTimeoutPushMessageTask()
        {
            _scheduleService.StartTask($"{Name}.{GetType().Name}.ScanTimeoutPushMessage", ScanTimeoutPushMessage, 1000, _setting.ScanTimeoutRequestInterval);
        }
        private void StopScanTimeoutPushMessageTask()
        {
            _scheduleService.StopTask($"{Name}.{GetType().Name}.ScanTimeoutPushMessage");
        }

        private void ScanTimeoutPushMessage()
        {
            var timeoutKeyList = new List<string>();
            foreach (var entry in _pushResponseFutureDict)
            {
                if (entry.Value.IsTimeout())
                {
                    timeoutKeyList.Add(entry.Key);
                }
            }
            foreach (var key in timeoutKeyList)
            {
                if (_pushResponseFutureDict.TryRemove(key, out PushResponseFuture pushResponseFuture))
                {
                    var pushMessage = pushResponseFuture.PushMessage;
                    var response = PushResponseMessage.BuildExceptionPushResponse(pushMessage, "Push message timeout.");
                    pushResponseFuture.SetResponse(response);
                    _logger.LogDebug($"Removed timeout pushMessage, name: {Name}, pushMessageId: {pushResponseFuture.PushMessage.Id}");
                }
            }
        }

        private void HandlePushResponseMessage(PushResponseMessage pushResponseMessage)
        {
            if (_pushResponseFutureDict.TryRemove(pushResponseMessage.RequestId, out PushResponseFuture pushResponseFuture))
            {

                if (pushResponseFuture.SetResponse(pushResponseMessage))
                {
                    _logger.LogDebug($"PushMessage response back, name: {Name}, pushMessage code: {pushResponseFuture.PushMessage.Code}, pushMessage id: {pushResponseFuture.PushMessage.Id}, time spent: {(DateTime.Now - pushResponseFuture.BeginTime).TotalMilliseconds}");
                }
                else
                {
                    _logger.LogError($"Set pushMessage response failed, name: {Name}, responseId: {pushResponseMessage.Id}");
                }
            }
        }

    }
}
