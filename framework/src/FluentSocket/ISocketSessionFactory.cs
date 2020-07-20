namespace FluentSocket.Impl
{
    public interface ISocketSessionFactory
    {
        /// <summary>Add session
        /// </summary>
        void AddSession(ISocketSession socketSession);

        /// <summary>Add or update session
        /// </summary>
        void UpdateSession(ISocketSession socketSession);

        /// <summary>Get Session by sessionId
        /// </summary>
        ISocketSession GetSession(string sessionId);

        /// <summary>Remove session by sessionId
        /// </summary>
        void RemoveSession(string sessionId);

        /// <summary>ClearSession
        /// </summary>
        void ClearSession();
    }
}
