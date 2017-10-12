using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ricotta.Transport
{
    public class ClientAuthInfoCache
    {
        private ConcurrentDictionary<string, ClientAuthInfo> _clients;      // <RSAFingerprint, ClientAuthInfo>
        private ConcurrentDictionary<string, ClientAuthInfo> _clientsById;  // <ClientId, ClientAuthInfo>

        public ClientAuthInfoCache()
        {
            _clients = new ConcurrentDictionary<string, ClientAuthInfo>();
            _clientsById = new ConcurrentDictionary<string, ClientAuthInfo>();
        }

        public ClientAuthInfo Get(string rsaFingerprint)
        {
            if (_clients.ContainsKey(rsaFingerprint))
            {
                return _clients[rsaFingerprint];
            }
            return null;
        }

        public ClientAuthInfo GetById(string clientId)
        {
            if (_clientsById.ContainsKey(clientId))
            {
                return _clientsById[clientId];
            }
            return null;
        }

        public List<ClientAuthInfo> GetList()
        {
            return _clients.Values.ToList();
        }

        private void SetClientStatus(string rsaFingerprint, ClientStatus clientStatus)
        {
            if (_clients.ContainsKey(rsaFingerprint))
            {
                _clients[rsaFingerprint].AuthenticationStatus = clientStatus;
            }
        }

        private void SetClientStatusById(string clientId, ClientStatus clientStatus)
        {
            if (_clientsById.ContainsKey(clientId))
            {
                _clientsById[clientId].AuthenticationStatus = clientStatus;
            }
        }

        public ClientAuthInfo Add(string rsaFingerprint, string clientId, ClientStatus clientStatus = ClientStatus.Pending)
        {
            var clientAuthInfo = Get(rsaFingerprint);
            if (clientAuthInfo != null)
            {
                if (clientAuthInfo.ClientId != clientId && _clientsById.ContainsKey(clientAuthInfo.ClientId))
                {
                    ClientAuthInfo removed = null;
                    _clientsById.TryRemove(clientAuthInfo.ClientId, out removed);
                }
                clientAuthInfo.ClientId = clientId;
                clientAuthInfo.AuthenticationStatus = clientStatus;
                _clientsById.TryAdd(clientId, clientAuthInfo);
                return clientAuthInfo;
            }
            else
            {
                var newClientAuthInfo = new ClientAuthInfo
                {
                    RsaFingerprint = rsaFingerprint,
                    ClientId = clientId,
                    AuthenticationStatus = clientStatus
                };
                _clients.TryAdd(rsaFingerprint, newClientAuthInfo);
                _clientsById.TryAdd(clientId, newClientAuthInfo);
                return newClientAuthInfo;
            }
        }

        public void Accept(string rsaFingerprint)
        {
            SetClientStatus(rsaFingerprint, ClientStatus.Accepted);
        }

        public void AcceptById(string clientId)
        {
            SetClientStatusById(clientId, ClientStatus.Accepted);
        }

        public void Deny(string rsaFingerprint)
        {
            SetClientStatus(rsaFingerprint, ClientStatus.Denied);
        }

        public void DenyById(string clientId)
        {
            SetClientStatusById(clientId, ClientStatus.Denied);
        }

        public void Pending(string rsaFingerprint)
        {
            SetClientStatus(rsaFingerprint, ClientStatus.Pending);
        }

        public void PendingById(string clientId)
        {
            SetClientStatusById(clientId, ClientStatus.Pending);
        }

    }
}