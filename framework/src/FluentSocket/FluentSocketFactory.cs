using System;

namespace FluentSocket
{
    public class FluentSocketFactory : IFluentSocketFactory
    {
        private readonly IServiceProvider _provider;
        public FluentSocketFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public SocketServer CreateServer(ServerSetting setting)
        {
            return _provider.CreateInstance<SocketServer>(setting);
        }

        public SocketClient CreateClient(ClientSetting setting)
        {
            return _provider.CreateInstance<SocketClient>(setting);
        }
    }
}
