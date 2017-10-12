using System;
using System.Collections.Concurrent;

namespace Ricotta.Transport
{
    public class SessionCache
    {
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

        public Session NewCommandSession()
        {
            var session = new Session
            {
                Id = $"*{Guid.NewGuid().ToString()}"
            };
            _sessions.TryAdd(session.Id, session);
            return session;
        }

        public bool IsCommandSession(string sessionId)
        {
            return sessionId.StartsWith("*");
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