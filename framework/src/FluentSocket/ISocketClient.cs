using FluentSocket.Protocols;
using FluentSocket.Traffic;
using System.Net;
using System.Threading.Tasks;

namespace FluentSocket
{
    public interface ISocketClient
    {
        /// <summary>ServerEndPoint
        /// </summary>
        IPEndPoint ServerEndPoint { get; }

        /// <summary>LocalEndPoint
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>IsConnected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>Connect to server
        /// </summary>
        ValueTask ConnectAsync();

        /// <summary>Send message async
        /// </summary>
        ValueTask<ResponseMessage> SendMessageAsync(RequestMessage request, int timeoutMillis = 3000);

        /// <summary>Disconnect to server
        /// </summary>
        ValueTask CloseAsync();

        /// <summary>Register PushMessageHandler
        /// </summary>
        void RegisterPushHandler(short code, IPushMessageHandler handler);
    }
}
