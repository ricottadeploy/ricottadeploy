using System;
using NetMQ;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.SecurityLayer;
using Ricotta.Transport;
using NetMQ.Sockets;
using Serilog;

namespace Ricotta.Transport
{
    public class Server
    {
        public delegate byte[] OnApplicationDataReceivedCallback(byte[] data);
        private OnApplicationDataReceivedCallback _onApplicationDataReceivedCallback;
        private ISerializer _serializer;
        private Rsa _rsa;
        private Publisher _publisher;
        private NetMQSocket _socket;
        private readonly SessionCache _sessionCache;
        private readonly ClientAuthInfoCache _clientAuthInfoCache;

        public Server(ISerializer serializer,
                        Rsa rsa,
                        Publisher publisher,
                        SessionCache sessionCache,
                        ClientAuthInfoCache clientAuthInfoCache,
                        string serverUri)
        {
            _onApplicationDataReceivedCallback = new OnApplicationDataReceivedCallback(DefaultOnApplicationDataReceivedCallback);
            _serializer = serializer;
            _rsa = rsa;
            _publisher = publisher;
            _sessionCache = sessionCache;
            _clientAuthInfoCache = clientAuthInfoCache;
            // Accept requests coming from CLI. CLI uses master's RSA keys so add its fingerprint as accepted.
            _clientAuthInfoCache.Add(_rsa.Fingerprint, "!", ClientStatus.Accepted);
            _socket = new ResponseSocket();
            _socket.Connect(serverUri);
        }

        public void Listen()
        {
            while (true)
            {
                var messageBytes = _socket.ReceiveFrameBytes();
                var message = _serializer.Deserialize<SecurityLayerMessage>(messageBytes);
                switch (message.Type)
                {
                    case SecurityMessageType.ClientHello:
                        HandleClientHello(message.Data);
                        break;
                    case SecurityMessageType.ClientKeyExchange:
                        HandleClientKeyExchange(message.Data);
                        break;
                    case SecurityMessageType.ApplicationData:
                        HandleApplicationData(message.Data);
                        break;
                }
            }
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

        private byte[] Receive()
        {
            return _socket.ReceiveFrameBytes();
        }

        private void HandleClientHello(byte[] message)
        {
            var clientHello = _serializer.Deserialize<ClientHello>(message);
            var clientRsa = Rsa.CreateFromPublicPEM(clientHello.RSAPublicPem);
            Log.Debug($"Received ClientHello from {clientHello.ClientId} with RSA fingerprint {clientRsa.Fingerprint}");
            var clientAuthInfo = _clientAuthInfoCache.Get(clientRsa.Fingerprint);
            if (clientAuthInfo == null)
            {
                clientAuthInfo = _clientAuthInfoCache.Add(clientRsa.Fingerprint, clientHello.ClientId, ClientStatus.Pending);
            }
            Log.Debug($"{clientHello.ClientId} authentication status: {clientAuthInfo.AuthenticationStatus}");
            if (clientAuthInfo.AuthenticationStatus == ClientStatus.Accepted)
            {
                Session session = null;
                if (clientHello.ClientId == "!")
                {
                    session = _sessionCache.NewCommandSession();
                }
                else
                {
                    session = _sessionCache.NewSession();
                }
                var random = Tls12.NewRandom();
                session.ServerRandom = random;
                session.ClientRandom = clientHello.Random;
                session.RSAPublicPem = clientHello.RSAPublicPem;
                var serverHello = new ServerHello
                {
                    SessionId = session.Id,
                    Random = random,
                    RSAPublicPem = _rsa.PublicPem
                };
                var data = _serializer.Serialize<ServerHello>(serverHello);
                var response = new SecurityLayerMessage
                {
                    Type = SecurityMessageType.ServerHello,
                    Data = data
                };
                var responseBytes = _serializer.Serialize<SecurityLayerMessage>(response);
                Send(responseBytes);
            }
            else
            {
                var serverAuthenticationStatus = new ServerAuthenticationStatus
                {
                    Status = clientAuthInfo.AuthenticationStatus
                };
                var data = _serializer.Serialize<ServerAuthenticationStatus>(serverAuthenticationStatus);
                var response = new SecurityLayerMessage
                {
                    Type = SecurityMessageType.ServerAuthenticationStatus,
                    Data = data
                };
                var responseBytes = _serializer.Serialize<SecurityLayerMessage>(response);
                Send(responseBytes);
            }
        }

        private void HandleClientKeyExchange(byte[] message)
        {
            var decryptedMessage = _rsa.Decrypt(message);
            var clientKeyExchange = _serializer.Deserialize<ClientKeyExchange>(decryptedMessage);
            var session = _sessionCache.Get(clientKeyExchange.SessionId);
            if (session == null)
            {
                // TODO: Send error
            }
            else
            {
                session.MasterSecret = Tls12.GetMasterSecret(clientKeyExchange.PreMasterSecret, session.ClientRandom, session.ServerRandom);
                var keys = Tls12.GetKeys(session.MasterSecret, session.ClientRandom, session.ServerRandom);
                session.ClientWriteMACKey = Tls12.GetClientWriteMACKey(keys);
                session.ServerWriteMACKey = Tls12.GetServerWriteMACKey(keys);
                session.ClientWriteKey = Tls12.GetClientWriteKey(keys);
                session.ServerWriteKey = Tls12.GetServerWriteKey(keys);
                session.IsAuthenticated = true;
            }
            var serverFinished = new ServerFinished
            {
                SessionId = session.Id,
                PublishAesKey = _publisher.Aes.Key
            };
            var data = _serializer.Serialize<ServerFinished>(serverFinished);
            var response = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ServerFinished,
                Data = data
            };
            var responseBytes = _serializer.Serialize<SecurityLayerMessage>(response);
            Send(responseBytes);
        }

        private void HandleApplicationData(byte[] message)
        {
            var applicationData = _serializer.Deserialize<ApplicationData>(message);
            var session = _sessionCache.Get(applicationData.SessionId);
            if (!session.IsAuthenticated)
            {
                // TODO: Send error
            }
            else
            {
                var data = Aes.Decrypt(applicationData.Data, session.ClientWriteKey, applicationData.AesIv);
                var responseData = _onApplicationDataReceivedCallback(data);
                var aesIv = Tls12.NewAesIv();
                var applicationDataResponse = new ApplicationData
                {
                    SessionId = session.Id,
                    AesIv = aesIv,
                    Data = Aes.Encrypt(responseData, session.ServerWriteKey, aesIv)
                };
                var bytes = _serializer.Serialize<ApplicationData>(applicationDataResponse);
                Send(bytes);
            }
            if (_sessionCache.IsCommandSession(session.Id))
            {
                _sessionCache.Destroy(session.Id);
            }
        }

        public byte[] DefaultOnApplicationDataReceivedCallback(byte[] message)
        {
            return new byte[] { };
        }

        public void OnApplicationDataReceived(Func<byte[], byte[]> onApplicationDataReceivedCallback)
        {
            _onApplicationDataReceivedCallback = new OnApplicationDataReceivedCallback(onApplicationDataReceivedCallback);
        }
    }
}