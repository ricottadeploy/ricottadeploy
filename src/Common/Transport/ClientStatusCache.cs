using System;
using System.Collections.Concurrent;

namespace Ricotta.Transport
{
    public class ClientStatusCache
    {
        private ConcurrentDictionary<string, ClientStatus> _clients;    // <RSAFingerprint, ClientStatus>

        public ClientStatusCache()
        {
            _clients = new ConcurrentDictionary<string, ClientStatus>();
        }

        public ClientStatus? Get(string rsaFingerprint)
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
                _clients[rsaFingerprint] = clientStatus;
            }
            else
            {
                _clients.TryAdd(rsaFingerprint, clientStatus);
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