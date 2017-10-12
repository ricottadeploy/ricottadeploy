using System;
using System.Collections.Concurrent;

namespace Ricotta.Transport
{
    /// <summary>
    /// Used to store sessions. 
    /// Sessions for clients and CLI are different. This is necessary as CLI sessions need to be destroyed as soon as the server
    /// returns the response.
    /// </summary>
    public class SessionCache
    {
        private const string CLI_SESSION_PREFIX = "*";
        private ConcurrentDictionary<string, Session> _sessions;

        public SessionCache()
        {
            _sessions = new ConcurrentDictionary<string, Session>();
        }

        public Session NewSession()
        {
            var session = new Session
            {
                Id = Guid.NewGuid().ToString()
            };
            _sessions.TryAdd(session.Id, session);
            return session;
        }

        public Session NewCliSession()
        {
            var session = new Session
            {
                Id = $"{CLI_SESSION_PREFIX}{Guid.NewGuid().ToString()}"
            };
            _sessions.TryAdd(session.Id, session);
            return session;
        }

        public bool IsCliSession(string sessionId)
        {
            return sessionId.StartsWith(CLI_SESSION_PREFIX);
        }

        public Session Get(string sessionId)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                return _sessions[sessionId];
            }
            return null;
        }

        public bool Destroy(string sessionId)
        {
            Session removedSession;
            return _sessions.TryRemove(sessionId, out removedSession);
        }

        public void Clear()
        {
            _sessions = new ConcurrentDictionary<string, Session>();
        }
    }
}