using System;
using System.Collections.Concurrent;

namespace Ricotta.Transport
{
    public class ClientAuthInfoCache
    {
        private ConcurrentDictionary<string, ClientAuthInfo> _clients;    // <RSAFingerprint, ClientAuthInfo>

        public ClientAuthInfoCache()
        {
            _clients = new ConcurrentDictionary<string, ClientAuthInfo>();
        }

        public ClientAuthInfo Get(string rsaFingerprint)
        {
            if (_clients.ContainsKey(rsaFingerprint))
            {
                return _clients[rsaFingerprint];
            }
            return null;
        }

        private void SetClientStatus(string rsaFingerprint, ClientStatus clientStatus)
        {
            if (_clients.ContainsKey(rsaFingerprint))
            {
                _clients[rsaFingerprint].AuthenticationStatus = clientStatus;
            }
        }

        public void Accept(string rsaFingerprint)
        {
            SetClientStatus(rsaFingerprint, ClientStatus.Accepted);
        }

        public void Deny(string rsaFingerprint)
        {
            SetClientStatus(rsaFingerprint, ClientStatus.Denied);
        }

        public void Pending(string rsaFingerprint)
        {
            SetClientStatus(rsaFingerprint, ClientStatus.Pending);
        }

    }
}