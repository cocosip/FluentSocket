using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;

namespace FluentSocket
{
    public class FluentSocketFactory : IFluentSocketFactory
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public FluentSocketFactory(ILogger<FluentSocketFactory> logger, IServiceProvider serviceProvider)
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
