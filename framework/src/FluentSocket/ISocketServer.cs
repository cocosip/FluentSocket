using System.Net;

namespace FluentSocket
{
    public interface ISocketServer
    {
        /// <summary>Listening ip address and port
        /// </summary>
        IPEndPoint ListeningEndPoint { get; }
    }
}
