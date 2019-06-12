namespace FluentSocket
{
    public interface IFluentSocketFactory
    {

        SocketServer CreateServer(ServerSetting setting);

        SocketClient CreateClient(ClientSetting setting);
    }
}
