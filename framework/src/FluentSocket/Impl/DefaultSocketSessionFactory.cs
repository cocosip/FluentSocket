using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FluentSocket.Impl
{
    public class DefaultSocketSessionFactory : ISocketSessionFactory
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, ISocketSession> _sessionDict;

        public DefaultSocketSessionFactory(ILogger<DefaultSocketSessionFactory> logger)
        {
            _logger = logger;
            _sessionDict = new ConcurrentDictionary<string, ISocketSession>();
        }

        /// <summary>Add session
        /// </summary>
        public void AddOrUpdateSession(ISocketSession socketSession)
        {
            _sessionDict.AddOrUpdate(socketSession.SessionId, socketSession, (id, session) => socketSession);
        }

        /// <summary>Add or update session
        /// </summary>
        public void UpdateSession(ISocketSession socketSession)
        {
            if (_sessionDict.TryGetValue(socketSession.SessionId, out ISocketSession oldSession))
            {
                _sessionDict.TryUpdate(socketSession.SessionId, socketSession, oldSession);
            }
        }

        /// <summary>Get Session by sessionId
        /// </summary>
        public ISocketSession GetSession(string sessionId)
        {
            if (!_sessionDict.TryGetValue(sessionId, out ISocketSession session))
            {
                _logger.LogInformation("Get session fail,SessionId:{0}.", sessionId);
            }
            return session;
        }

        /// <summary>Get sessions
        /// </summary>
        public List<ISocketSession> GetSessions(Func<ISocketSession, bool> predicate)
        {
            return _sessionDict.Values.Where(predicate).ToList();
        }

        /// <summary>Get all sessions
        /// </summary>
        public List<ISocketSession> GetAllSessions()
        {
            return _sessionDict.Values.ToList();
        }

        /// <summary>Remove session by sessionId
        /// </summary>
        public void RemoveSession(string sessionId)
        {
            if (!_sessionDict.TryRemove(sessionId, out ISocketSession _))
            {
                _logger.LogInformation("Remove session fail,SessionId:{0}", sessionId);
            }
        }

        /// <summary>ClearSession
        /// </summary>
        public void ClearSession()
        {
            _sessionDict.Clear();
        }
    }
}
