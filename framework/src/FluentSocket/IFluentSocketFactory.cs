namespace FluentSocket
{
    public interface IFluentSocketFactory
    {
        ISocketServer CreateServer(ServerSetting setting);

        ISocketServer CreateServer(int port);

        ISocketClient CreateClient(ClientSetting setting);

        ISocketClient CreateClient(string serverIP, int serverPort);
    }
}
