using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;

namespace FluentSocket
{
    public class DefaultFluentSocketFactory : IFluentSocketFactory
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public DefaultFluentSocketFactory(ILogger<DefaultFluentSocketFactory> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }


        public ISocketServer CreateServer(ServerSetting setting)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var injectSetting = scope.ServiceProvider.GetRequiredService<ServerSetting>();

                if (setting.ExtraSettings.Any())
                {
                    injectSetting.ExtraSettings.AddRange(setting.ExtraSettings);
                }


                injectSetting.ScanTimeoutRequestInterval = setting.ScanTimeoutRequestInterval;
                injectSetting.SendMessageFlowControlThreshold = setting.SendMessageFlowControlThreshold;
                injectSetting.QuietPeriodMilliSeconds = setting.QuietPeriodMilliSeconds;
                injectSetting.CloseTimeoutSeconds = setting.CloseTimeoutSeconds;

                injectSetting.ListeningEndPoint = setting.ListeningEndPoint;
                injectSetting.MessageReqCapacity = setting.MessageReqCapacity;
                injectSetting.HandleMessageReqThread = setting.HandleMessageReqThread;

                //var injectServiceProvider = _serviceProvider.GetService<IServiceProvider>();


                var socketServer = scope.ServiceProvider.GetRequiredService<ISocketServer>();
                return socketServer;
            }
        }

        public ISocketServer CreateServer(int port)
        {
            var setting = new ServerSetting()
            {
                ListeningEndPoint = new IPEndPoint(IPAddress.Any, port)
            };

            return CreateServer(setting);
        }

        public ISocketClient CreateClient(ClientSetting setting)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var injectSetting = scope.ServiceProvider.GetRequiredService<ClientSetting>();

                if (setting.ExtraSettings.Any())
                {
                    injectSetting.ExtraSettings.AddRange(setting.ExtraSettings);
                }

                injectSetting.ScanTimeoutRequestInterval = setting.ScanTimeoutRequestInterval;
                injectSetting.SendMessageFlowControlThreshold = setting.SendMessageFlowControlThreshold;
                injectSetting.QuietPeriodMilliSeconds = setting.QuietPeriodMilliSeconds;
                injectSetting.CloseTimeoutSeconds = setting.CloseTimeoutSeconds;

                injectSetting.ServerEndPoint = setting.ServerEndPoint;
                injectSetting.LocalEndPoint = setting.LocalEndPoint;
                injectSetting.EnableReConnect = setting.EnableReConnect;
                injectSetting.ReConnectDelaySeconds = setting.ReConnectDelaySeconds;
                injectSetting.ReConnectMaxCount = setting.ReConnectMaxCount;
                injectSetting.EnableHeartbeat = setting.EnableHeartbeat;
                injectSetting.HeartbeatInterval = setting.HeartbeatInterval;
                injectSetting.PushReqCapacity = setting.PushReqCapacity;
                injectSetting.HandlePushReqThread = setting.HandlePushReqThread;

                var socketClient = scope.ServiceProvider.GetRequiredService<ISocketClient>();
                return socketClient;
            }
        }

        public ISocketClient CreateClient(string serverIP, int serverPort)
        {
            var setting = new ClientSetting()
            {
                ServerEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort)
            };

            return CreateClient(setting);
        }


    }
}
