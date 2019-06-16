using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using FluentSocket.Codecs;
using FluentSocket.Extensions;
using FluentSocket.Handlers;
using FluentSocket.Traffic;
using FluentSocket.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSocket
{
    public class SocketClient
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly IScheduleService _scheduleService;


        private readonly ClientSetting _setting;
        public string Name { get; }
        public string ServerIPAddress { get; }
        public bool IsRunning { get { return _isRunning; } }
        public string LocalIPAddress { get { return _clientChannel?.LocalAddress.ToStringAddress() ?? ""; } }
        private IEventLoopGroup _group;
        private IChannel _clientChannel;
        private Bootstrap _bootStrap;

        private bool _isRunning = false;
        private int _reConnectAttempt = 0;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ManualResetEventSlim _manualResetEventSlim = new ManualResetEventSlim(false);
        private readonly Dictionary<int, IPushMessageHandler> _pushMessageHandlerDict;
        private readonly ConcurrentDictionary<string, ResponseFuture> _responseFutureDict;

        public SocketClient(IServiceProvider provider, ILoggerFactory loggerFactory, IScheduleService scheduleService, ClientSetting setting)
        {
            _provider = provider;
            _logger = loggerFactory.CreateLogger(FluentSocketSettings.LoggerName);
            _scheduleService = scheduleService;
            _setting = setting;
            Name = "SocketClient-" + ObjectId.GenerateNewStringId();
            ServerIPAddress = _setting.ServerEndPoint.ToIPv4Address();
            _pushMessageHandlerDict = new Dictionary<int, IPushMessageHandler>();
            _responseFutureDict = new ConcurrentDictionary<string, ResponseFuture>();
        }

        /// <summary>Run
        /// </summary>
        public async Task RunAsync()
        {
            if (_clientChannel != null && _clientChannel.Registered)
            {
                _logger.LogInformation($"Client is running! Don't run again! ChannelId:{_clientChannel.Id.AsLongText()}");
                return;
            }

            try
            {
                _group = new MultithreadEventLoopGroup(_setting.GroupEventLoopCount);
                _bootStrap = new Bootstrap();
                _bootStrap.Group(_group);
                _bootStrap
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, _setting.TcpNodelay)
                    .Option(ChannelOption.SoKeepalive, _setting.SoKeepalive)
                    .Option(ChannelOption.WriteBufferHighWaterMark, _setting.WriteBufferHighWaterMark)
                    .Option(ChannelOption.WriteBufferLowWaterMark, _setting.WriteBufferHighWaterMark)
                    .Option(ChannelOption.SoRcvbuf, _setting.SoRcvbuf)
                    .Option(ChannelOption.SoSndbuf, _setting.SoSndbuf)
                    .Option(ChannelOption.SoReuseaddr, _setting.SoReuseaddr)
                    .Option(ChannelOption.AutoRead, _setting.AutoRead)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (_setting.IsSsl && _setting.TlsCertificate != null)
                        {
                            var targetHost = _setting.TlsCertificate.GetNameInfo(X509NameType.DnsName, false);

                            pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast(new IdleStateHandler(_setting.ReaderIdleTimeSeconds, _setting.WriterIdleTimeSeconds, _setting.AllIdleTimeSeconds));
                        }

                        pipeline.AddLast(new LoggingHandler(FluentSocketSettings.LoggerName));
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));

                        pipeline.AddLast(new MessageDecoder(), new MessageEncoder());

                        if (_setting.EnableHeartbeat)
                        {
                            pipeline.AddLast("heartbeat", _provider.CreateInstance<HeartbeatHandler>());
                        }

                        //Set request response
                        pipeline.AddLast("request-response", _provider.CreateInstance<ResponseMessageHandler>(new Action<ResponseMessage>(HandleResponseMessage)));

                        pipeline.AddLast("channel-watcher", _provider.CreateInstance<ChannelWatcherHandler>(new Action<bool>(ChannelWritableChanged)));

                        //PushMessage Code Handler
                        AddPushMessageHandler(pipeline);

                        if (_setting.EnableReConnect)
                        {
                            //Reconnect to server
                            pipeline.AddLast("reconnect", _provider.CreateInstance<ReConnectHandler>(_setting, new Func<Task>(DoReConnectIfNeed)));
                        }
                        _setting.PipelineConfigure?.Invoke(pipeline);

                    }));

                //Connect
                await DoConnect();
                _isRunning = true;
                //Scan timeout request
                StartScanTimeoutRequestTask();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                StopScanTimeoutRequestTask();
                await _group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds));
            }
        }

        /// <summary>Close
        /// </summary>
        public async Task CloseAsync()
        {
            if (_clientChannel == null)
            {
                return;
            }
            try
            {
                _isRunning = false;
                await _clientChannel.CloseAsync();
            }
            finally
            {
                StopScanTimeoutRequestTask();
                await _group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(_setting.QuietPeriodMilliSeconds), TimeSpan.FromSeconds(_setting.CloseTimeoutSeconds));
            }
        }

        /// <summary>Send message, return ResponseMessage
        /// </summary>
        public Task<ResponseMessage> SendAsync(RequestMessage request, int timeoutMillis, int thresholdCount = 1000, bool sendWait = true)
        {
            CheckChannel(_clientChannel);
            if (!_clientChannel.IsWritable)
            {
                _manualResetEventSlim.Wait();
            }
            var sleepMilliseconds = FlowControlUtil.CalculateFlowControlTimeMilliseconds(_responseFutureDict.Count, thresholdCount);
            if (sleepMilliseconds > 0)
            {
                Thread.Sleep(sleepMilliseconds);
            }
            var taskCompletionSource = new TaskCompletionSource<ResponseMessage>();
            var responseFuture = new ResponseFuture(request, timeoutMillis, taskCompletionSource);
            if (!_responseFutureDict.TryAdd(request.Id, responseFuture))
            {
                throw new Exception($"Add remoting request response future failed. request id:{request.Id}");
            }
            if (sendWait)
            {
                _clientChannel.WriteAndFlushAsync(request).Wait();
            }
            else
            {
                _clientChannel.WriteAndFlushAsync(request);
            }
            return taskCompletionSource.Task;
        }


        /// <summary>Register PushMessageHandler
        /// </summary>
        public SocketClient RegisterPushMessageHandler(int code, IPushMessageHandler pushMessageHandler)
        {
            _pushMessageHandlerDict[code] = pushMessageHandler;
            return this;
        }

        /// <summary>DoConnect to the server
        /// </summary>
        private async Task DoConnect()
        {
            _clientChannel = _setting.LocalEndPoint == null ? await _bootStrap.ConnectAsync(_setting.ServerEndPoint) : await _bootStrap.ConnectAsync(_setting.ServerEndPoint, _setting.LocalEndPoint);
            _logger.LogInformation($"Client DoConnect! name:{Name},serverEndPoint:{_clientChannel.RemoteAddress.ToStringAddress()},localAddress:{_clientChannel.LocalAddress.ToStringAddress()}");
        }

        private async Task DoReConnectIfNeed()
        {
            if (!_setting.EnableReConnect || _setting.ReConnectMaxCount < _reConnectAttempt)
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
                    await DoConnect();
                    Interlocked.Exchange(ref _reConnectAttempt, 0);
                    reConnectSuccess = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("ReConnect fail!{0}", ex.Message);
                }
                finally
                {
                    Interlocked.Increment(ref _reConnectAttempt);
                    _semaphoreSlim.Release();
                }
                //Try again!
                if (_reConnectAttempt < _setting.ReConnectMaxCount && !reConnectSuccess)
                {
                    Thread.Sleep(_setting.ReConnectIntervalMilliSeconds);
                    await DoReConnectIfNeed();
                }
            }
        }

        private void ChannelWritableChanged(bool isWriteable)
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

        private void StartScanTimeoutRequestTask()
        {
            _scheduleService.StartTask($"{Name}.{GetType().Name}.ScanTimeoutRequest", ScanTimeoutRequest, 1000, _setting.ScanTimeoutRequestInterval);
        }

        private void StopScanTimeoutRequestTask()
        {
            _scheduleService.StopTask($"{Name}.{GetType().Name}.ScanTimeoutRequest");
        }

        private void ScanTimeoutRequest()
        {
            var timeoutKeyList = new List<string>();
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
                    var request = responseFuture.Request;
                    responseFuture.SetResponse(ResponseMessage.BuildExceptionResponse(request, "Remoting request timeout."));
                    _logger.LogInformation($"Removed timeout request, name: {Name}, requestId: {responseFuture.Request.Id}");
                }
            }
        }

        private void HandleResponseMessage(ResponseMessage responseMessage)
        {
            if (_responseFutureDict.TryRemove(responseMessage.RequestId, out ResponseFuture responseFuture))
            {
                if (responseFuture.SetResponse(responseMessage))
                {
                    _logger.LogDebug($"Remoting response back, name: {Name}, request code: {responseFuture.Request.Code}, requect id: {responseFuture.Request.Id}, time spent: {(DateTime.Now - responseFuture.BeginTime).TotalMilliseconds},beginTime:{responseFuture.BeginTime.ToString("yyyy-MM-dd HH:mm:ss fff")}");
                }
                else
                {
                    _logger.LogDebug($"Set remoting response failed, name: {Name}, responseId: {responseMessage.Id}");
                }
            }
        }

        private void AddPushMessageHandler(IChannelPipeline pipeline)
        {
            _setting.PushMessageHandlerConfigure?.Invoke(_pushMessageHandlerDict);
            var pushHandler = _provider.CreateInstance<PushHandler>(_setting);
            foreach (var item in _pushMessageHandlerDict)
            {
                pushHandler.RegisterPushMessageHandler(item.Key, item.Value);
            }
            pipeline.AddLast("push", pushHandler);
        }
    }
}
