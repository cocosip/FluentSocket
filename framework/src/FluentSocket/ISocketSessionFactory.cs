using System;
using System.Collections.Generic;

namespace FluentSocket.Impl
{
    public interface ISocketSessionFactory
    {
        /// <summary>Add session
        /// </summary>
        void AddOrUpdateSession(ISocketSession socketSession);

        /// <summary>Add or update session
        /// </summary>
        void UpdateSession(ISocketSession socketSession);

        /// <summary>Get Session by sessionId
        /// </summary>
        ISocketSession GetSession(string sessionId);

        /// <summary>Get all sessions
        /// </summary>
        List<ISocketSession> GetAllSessions();

        /// <summary>Get sessions
        /// </summary>
        List<ISocketSession> GetSessions(Func<ISocketSession, bool> predicate);

        /// <summary>Remove session by sessionId
        /// </summary>
        ISocketSession RemoveSession(string sessionId);

        /// <summary>ClearSession
        /// </summary>
        void ClearSession();
    }
}
